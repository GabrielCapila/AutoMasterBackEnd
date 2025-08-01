using System.Text.Json;
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
                if (entry.Field != "comments" || entry.Value == null)
                    continue;

                var account = await _context.InstagramAccounts
                    .FirstOrDefaultAsync(a => a.InstagramUserId == entry.Id);
                if (account == null || string.IsNullOrEmpty(account.AccessToken))
                    continue;

                var comment = entry.Value;
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
                    WebhookData = JsonSerializer.Serialize(entry)
                };

                _context.CommentEvents.Add(commentEvent);
                await _context.SaveChangesAsync();

                const string msg = "Thanks for sharing!";
                await _instagram.PostCommentReplyAsync(account.AccessToken!, comment.Id, msg);
                await _instagram.SendPrivateMessageAsync(account.AccessToken!, comment.From.Id, msg);
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

}
