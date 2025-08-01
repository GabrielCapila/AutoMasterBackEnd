using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstagramAutomation.Api.Data;
using InstagramAutomation.Api.DTOs;
using InstagramAutomation.Api.Models;
using InstagramAutomation.Api.Services;

namespace InstagramAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IInstagramApiService _instagram;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IConfiguration configuration,
        ApplicationDbContext context,
        IInstagramApiService instagram,
        ILogger<WebhookController> logger)
    {
        _configuration = configuration;
        _context = context;
        _instagram = instagram;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string token)
    {
        var verifyToken = _configuration["Instagram:WebhookVerifyToken"];
        if (mode == "subscribe" && token == verifyToken)
        {
            return Ok(challenge);
        }
        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        var requests = ParseRequests(payload);

        foreach (var request in requests)
        {
            if (request.Entry == null)
                continue;

            foreach (var entry in request.Entry)
            {
                var account = await _context.InstagramAccounts
                    .FirstOrDefaultAsync(a => a.InstagramUserId == entry.Id);
                if (account == null)
                    continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Field != "comments")
                        continue;

                    var comment = change.Value;
                    var commentEvent = new CommentEvent
                    {
                        InstagramAccountId = account.Id,
                        CommentId = comment.Id,
                        MediaId = comment.Media.Id,
                        CommenterId = comment.From.Id,
                        CommenterUsername = comment.From.Username,
                        CommentText = comment.Text,
                        CommentTimestamp = DateTimeOffset.FromUnixTimeSeconds(entry.Time).UtcDateTime,
                        MediaType = comment.Media.MediaType,
                        WebhookData = JsonSerializer.Serialize(change)
                    };

                    _context.CommentEvents.Add(commentEvent);
                    await _context.SaveChangesAsync();

                    await ProcessRules(account, commentEvent);
                }
            }
        }

        return Ok();
    }

    internal static List<WebhookRequest> ParseRequests(JsonElement payload)
    {
        var requests = new List<WebhookRequest>();
        if (payload.ValueKind == JsonValueKind.Array)
        {
            var array = JsonSerializer.Deserialize<List<WebhookRequest>>(payload.GetRawText());
            if (array != null)
                requests.AddRange(array);
        }
        else if (payload.ValueKind == JsonValueKind.Object)
        {
            var obj = JsonSerializer.Deserialize<WebhookRequest>(payload.GetRawText());
            if (obj != null)
                requests.Add(obj);
        }
        return requests;
    }

    private async Task ProcessRules(InstagramAccount account, CommentEvent commentEvent)
    {
        if (string.IsNullOrEmpty(account.AccessToken))
            return;

        var rules = await _context.AutomationRules
            .Where(r => r.InstagramAccountId == account.Id && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        foreach (var rule in rules)
        {
            if (!IsMatch(commentEvent.CommentText, rule))
                continue;

            if (!string.IsNullOrEmpty(rule.PublicResponse))
            {
                var exec = new ActionExecution
                {
                    CommentEventId = commentEvent.Id,
                    AutomationRuleId = rule.Id,
                    ActionType = "public_reply",
                    Status = "pending",
                    ResponseText = rule.PublicResponse,
                    CreatedAt = DateTime.UtcNow
                };

                var success = await _instagram.PostCommentReplyAsync(account.AccessToken!, commentEvent.CommentId, rule.PublicResponse!);
                exec.Status = success ? "success" : "failed";
                exec.ExecutedAt = DateTime.UtcNow;
                _context.ActionExecutions.Add(exec);
            }

            if (rule.SendPrivateMessage && !string.IsNullOrEmpty(rule.PrivateMessage))
            {
                var exec = new ActionExecution
                {
                    CommentEventId = commentEvent.Id,
                    AutomationRuleId = rule.Id,
                    ActionType = "private_message",
                    Status = "pending",
                    ResponseText = rule.PrivateMessage,
                    CreatedAt = DateTime.UtcNow
                };

                var success = await _instagram.SendPrivateMessageAsync(account.AccessToken!, commentEvent.CommenterId, rule.PrivateMessage!);
                exec.Status = success ? "success" : "failed";
                exec.ExecutedAt = DateTime.UtcNow;
                _context.ActionExecutions.Add(exec);
            }

            rule.ExecutionCount++;
            rule.LastExecuted = DateTime.UtcNow;
            commentEvent.Processed = true;
            commentEvent.ProcessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private static bool IsMatch(string text, AutomationRule rule)
    {
        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var keywords = rule.TriggerKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var kw in keywords)
        {
            if (rule.MatchType == "exact")
            {
                if (string.Equals(text, kw, comparison))
                    return true;
            }
            else if (rule.MatchType == "partial")
            {
                if (text.Contains(kw, comparison))
                    return true;
            }
            else if (rule.MatchType == "regex")
            {
                var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (Regex.IsMatch(text, kw, options))
                    return true;
            }
            else if (rule.MatchType == "fuzzy")
            {
                if (text.Contains(kw, comparison))
                    return true;
            }
        }
        return false;
    }
}
