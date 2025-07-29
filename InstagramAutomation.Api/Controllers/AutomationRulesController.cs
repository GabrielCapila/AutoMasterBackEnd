using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InstagramAutomation.Api.Data;
using InstagramAutomation.Api.DTOs;
using InstagramAutomation.Api.Models;

namespace InstagramAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AutomationRulesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AutomationRulesController> _logger;

    public AutomationRulesController(ApplicationDbContext context, ILogger<AutomationRulesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AutomationRuleResponse>>> GetRules()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var rules = await _context.AutomationRules
            .Include(r => r.InstagramAccount)
            .Where(r => r.UserId == userId)
            .Select(r => new AutomationRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                TriggerKeywords = r.TriggerKeywords,
                MatchType = r.MatchType,
                CaseSensitive = r.CaseSensitive,
                FuzzyThreshold = r.FuzzyThreshold,
                PublicResponse = r.PublicResponse,
                PrivateMessage = r.PrivateMessage,
                SendPrivateMessage = r.SendPrivateMessage,
                IsActive = r.IsActive,
                Priority = r.Priority,
                MaxExecutionsPerHour = r.MaxExecutionsPerHour,
                MaxExecutionsPerDay = r.MaxExecutionsPerDay,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                LastExecuted = r.LastExecuted,
                ExecutionCount = r.ExecutionCount,
                InstagramAccount = new InstagramAccountResponse
                {
                    Id = r.InstagramAccount.Id,
                    InstagramUserId = r.InstagramAccount.InstagramUserId,
                    Username = r.InstagramAccount.Username,
                    DisplayName = r.InstagramAccount.DisplayName,
                    ProfilePictureUrl = r.InstagramAccount.ProfilePictureUrl,
                    IsActive = r.InstagramAccount.IsActive,
                    CreatedAt = r.InstagramAccount.CreatedAt,
                    LastSync = r.InstagramAccount.LastSync,
                    FollowerCount = r.InstagramAccount.FollowerCount,
                    FollowingCount = r.InstagramAccount.FollowingCount,
                    MediaCount = r.InstagramAccount.MediaCount,
                    TokenValid = r.InstagramAccount.TokenExpiresAt == null || r.InstagramAccount.TokenExpiresAt > DateTime.UtcNow,
                    TokenExpiresAt = r.InstagramAccount.TokenExpiresAt
                }
            })
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<AutomationRuleResponse>> CreateRule(AutomationRuleRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            // Verificar se a conta Instagram pertence ao usuário
            var instagramAccount = await _context.InstagramAccounts
                .FirstOrDefaultAsync(a => a.Id == request.InstagramAccountId && a.UserId == userId);

            if (instagramAccount == null)
            {
                return BadRequest(new { message = "Conta Instagram não encontrada ou não pertence ao usuário" });
            }

            // Verificar se já existe uma regra com o mesmo nome para este usuário
            var existingRule = await _context.AutomationRules
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Name == request.Name);

            if (existingRule != null)
            {
                return Conflict(new { message = "Já existe uma regra com este nome" });
            }

            var rule = new AutomationRule
            {
                UserId = userId.Value,
                InstagramAccountId = request.InstagramAccountId,
                Name = request.Name,
                Description = request.Description,
                TriggerKeywords = request.TriggerKeywords,
                MatchType = request.MatchType,
                CaseSensitive = request.CaseSensitive,
                FuzzyThreshold = request.FuzzyThreshold,
                PublicResponse = request.PublicResponse,
                PrivateMessage = request.PrivateMessage,
                SendPrivateMessage = request.SendPrivateMessage,
                IsActive = request.IsActive,
                Priority = request.Priority,
                MaxExecutionsPerHour = request.MaxExecutionsPerHour,
                MaxExecutionsPerDay = request.MaxExecutionsPerDay
            };

            _context.AutomationRules.Add(rule);
            await _context.SaveChangesAsync();

            // Recarregar com dados da conta Instagram
            await _context.Entry(rule)
                .Reference(r => r.InstagramAccount)
                .LoadAsync();

            _logger.LogInformation("Nova regra de automação criada: {RuleName} para usuário {UserId}", rule.Name, userId);

            var response = new AutomationRuleResponse
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                TriggerKeywords = rule.TriggerKeywords,
                MatchType = rule.MatchType,
                CaseSensitive = rule.CaseSensitive,
                FuzzyThreshold = rule.FuzzyThreshold,
                PublicResponse = rule.PublicResponse,
                PrivateMessage = rule.PrivateMessage,
                SendPrivateMessage = rule.SendPrivateMessage,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                MaxExecutionsPerHour = rule.MaxExecutionsPerHour,
                MaxExecutionsPerDay = rule.MaxExecutionsPerDay,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt,
                LastExecuted = rule.LastExecuted,
                ExecutionCount = rule.ExecutionCount,
                InstagramAccount = new InstagramAccountResponse
                {
                    Id = instagramAccount.Id,
                    InstagramUserId = instagramAccount.InstagramUserId,
                    Username = instagramAccount.Username,
                    DisplayName = instagramAccount.DisplayName,
                    ProfilePictureUrl = instagramAccount.ProfilePictureUrl,
                    IsActive = instagramAccount.IsActive,
                    CreatedAt = instagramAccount.CreatedAt,
                    LastSync = instagramAccount.LastSync,
                    FollowerCount = instagramAccount.FollowerCount,
                    FollowingCount = instagramAccount.FollowingCount,
                    MediaCount = instagramAccount.MediaCount,
                    TokenValid = instagramAccount.TokenExpiresAt == null || instagramAccount.TokenExpiresAt > DateTime.UtcNow,
                    TokenExpiresAt = instagramAccount.TokenExpiresAt
                }
            };

            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar regra de automação para usuário {UserId}", userId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AutomationRuleResponse>> GetRule(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var rule = await _context.AutomationRules
            .Include(r => r.InstagramAccount)
            .Where(r => r.Id == id && r.UserId == userId)
            .Select(r => new AutomationRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                TriggerKeywords = r.TriggerKeywords,
                MatchType = r.MatchType,
                CaseSensitive = r.CaseSensitive,
                FuzzyThreshold = r.FuzzyThreshold,
                PublicResponse = r.PublicResponse,
                PrivateMessage = r.PrivateMessage,
                SendPrivateMessage = r.SendPrivateMessage,
                IsActive = r.IsActive,
                Priority = r.Priority,
                MaxExecutionsPerHour = r.MaxExecutionsPerHour,
                MaxExecutionsPerDay = r.MaxExecutionsPerDay,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                LastExecuted = r.LastExecuted,
                ExecutionCount = r.ExecutionCount,
                InstagramAccount = new InstagramAccountResponse
                {
                    Id = r.InstagramAccount.Id,
                    InstagramUserId = r.InstagramAccount.InstagramUserId,
                    Username = r.InstagramAccount.Username,
                    DisplayName = r.InstagramAccount.DisplayName,
                    ProfilePictureUrl = r.InstagramAccount.ProfilePictureUrl,
                    IsActive = r.InstagramAccount.IsActive,
                    CreatedAt = r.InstagramAccount.CreatedAt,
                    LastSync = r.InstagramAccount.LastSync,
                    FollowerCount = r.InstagramAccount.FollowerCount,
                    FollowingCount = r.InstagramAccount.FollowingCount,
                    MediaCount = r.InstagramAccount.MediaCount,
                    TokenValid = r.InstagramAccount.TokenExpiresAt == null || r.InstagramAccount.TokenExpiresAt > DateTime.UtcNow,
                    TokenExpiresAt = r.InstagramAccount.TokenExpiresAt
                }
            })
            .FirstOrDefaultAsync();

        if (rule == null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AutomationRuleResponse>> UpdateRule(int id, AutomationRuleRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var rule = await _context.AutomationRules
            .Include(r => r.InstagramAccount)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (rule == null)
        {
            return NotFound();
        }

        try
        {
            // Verificar se a conta Instagram pertence ao usuário
            if (request.InstagramAccountId != rule.InstagramAccountId)
            {
                var instagramAccount = await _context.InstagramAccounts
                    .FirstOrDefaultAsync(a => a.Id == request.InstagramAccountId && a.UserId == userId);

                if (instagramAccount == null)
                {
                    return BadRequest(new { message = "Conta Instagram não encontrada ou não pertence ao usuário" });
                }
            }

            // Verificar se já existe uma regra com o mesmo nome (exceto a atual)
            var existingRule = await _context.AutomationRules
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Name == request.Name && r.Id != id);

            if (existingRule != null)
            {
                return Conflict(new { message = "Já existe uma regra com este nome" });
            }

            // Atualizar regra
            rule.InstagramAccountId = request.InstagramAccountId;
            rule.Name = request.Name;
            rule.Description = request.Description;
            rule.TriggerKeywords = request.TriggerKeywords;
            rule.MatchType = request.MatchType;
            rule.CaseSensitive = request.CaseSensitive;
            rule.FuzzyThreshold = request.FuzzyThreshold;
            rule.PublicResponse = request.PublicResponse;
            rule.PrivateMessage = request.PrivateMessage;
            rule.SendPrivateMessage = request.SendPrivateMessage;
            rule.IsActive = request.IsActive;
            rule.Priority = request.Priority;
            rule.MaxExecutionsPerHour = request.MaxExecutionsPerHour;
            rule.MaxExecutionsPerDay = request.MaxExecutionsPerDay;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recarregar com dados atualizados
            await _context.Entry(rule)
                .Reference(r => r.InstagramAccount)
                .LoadAsync();

            _logger.LogInformation("Regra de automação atualizada: {RuleName} para usuário {UserId}", rule.Name, userId);

            var response = new AutomationRuleResponse
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                TriggerKeywords = rule.TriggerKeywords,
                MatchType = rule.MatchType,
                CaseSensitive = rule.CaseSensitive,
                FuzzyThreshold = rule.FuzzyThreshold,
                PublicResponse = rule.PublicResponse,
                PrivateMessage = rule.PrivateMessage,
                SendPrivateMessage = rule.SendPrivateMessage,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                MaxExecutionsPerHour = rule.MaxExecutionsPerHour,
                MaxExecutionsPerDay = rule.MaxExecutionsPerDay,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt,
                LastExecuted = rule.LastExecuted,
                ExecutionCount = rule.ExecutionCount,
                InstagramAccount = new InstagramAccountResponse
                {
                    Id = rule.InstagramAccount.Id,
                    InstagramUserId = rule.InstagramAccount.InstagramUserId,
                    Username = rule.InstagramAccount.Username,
                    DisplayName = rule.InstagramAccount.DisplayName,
                    ProfilePictureUrl = rule.InstagramAccount.ProfilePictureUrl,
                    IsActive = rule.InstagramAccount.IsActive,
                    CreatedAt = rule.InstagramAccount.CreatedAt,
                    LastSync = rule.InstagramAccount.LastSync,
                    FollowerCount = rule.InstagramAccount.FollowerCount,
                    FollowingCount = rule.InstagramAccount.FollowingCount,
                    MediaCount = rule.InstagramAccount.MediaCount,
                    TokenValid = rule.InstagramAccount.TokenExpiresAt == null || rule.InstagramAccount.TokenExpiresAt > DateTime.UtcNow,
                    TokenExpiresAt = rule.InstagramAccount.TokenExpiresAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar regra de automação {RuleId} para usuário {UserId}", id, userId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (rule == null)
        {
            return NotFound();
        }

        _context.AutomationRules.Remove(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Regra de automação removida: {RuleId} do usuário {UserId}", id, userId);

        return NoContent();
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<AutomationRuleResponse>> ToggleRule(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var rule = await _context.AutomationRules
            .Include(r => r.InstagramAccount)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (rule == null)
        {
            return NotFound();
        }

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Regra de automação {Status}: {RuleName} para usuário {UserId}", 
            rule.IsActive ? "ativada" : "desativada", rule.Name, userId);

        var response = new AutomationRuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            TriggerKeywords = rule.TriggerKeywords,
            MatchType = rule.MatchType,
            CaseSensitive = rule.CaseSensitive,
            FuzzyThreshold = rule.FuzzyThreshold,
            PublicResponse = rule.PublicResponse,
            PrivateMessage = rule.PrivateMessage,
            SendPrivateMessage = rule.SendPrivateMessage,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            MaxExecutionsPerHour = rule.MaxExecutionsPerHour,
            MaxExecutionsPerDay = rule.MaxExecutionsPerDay,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            LastExecuted = rule.LastExecuted,
            ExecutionCount = rule.ExecutionCount,
            InstagramAccount = new InstagramAccountResponse
            {
                Id = rule.InstagramAccount.Id,
                InstagramUserId = rule.InstagramAccount.InstagramUserId,
                Username = rule.InstagramAccount.Username,
                DisplayName = rule.InstagramAccount.DisplayName,
                ProfilePictureUrl = rule.InstagramAccount.ProfilePictureUrl,
                IsActive = rule.InstagramAccount.IsActive,
                CreatedAt = rule.InstagramAccount.CreatedAt,
                LastSync = rule.InstagramAccount.LastSync,
                FollowerCount = rule.InstagramAccount.FollowerCount,
                FollowingCount = rule.InstagramAccount.FollowingCount,
                MediaCount = rule.InstagramAccount.MediaCount,
                TokenValid = rule.InstagramAccount.TokenExpiresAt == null || rule.InstagramAccount.TokenExpiresAt > DateTime.UtcNow,
                TokenExpiresAt = rule.InstagramAccount.TokenExpiresAt
            }
        };

        return Ok(response);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }
}

