using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InstagramAutomation.Api.Data;
using InstagramAutomation.Api.DTOs;
using InstagramAutomation.Api.Models;
using InstagramAutomation.Api.Services;

namespace InstagramAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstagramAccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IInstagramApiService _instagramApiService;
    private readonly ILogger<InstagramAccountsController> _logger;

    public InstagramAccountsController(
        ApplicationDbContext context,
        IInstagramApiService instagramApiService,
        ILogger<InstagramAccountsController> logger)
    {
        _context = context;
        _instagramApiService = instagramApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InstagramAccountResponse>>> GetAccounts()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var accounts = await _context.InstagramAccounts
            .Where(a => a.UserId == userId)
            .Select(a => new InstagramAccountResponse
            {
                Id = a.Id,
                InstagramUserId = a.InstagramUserId,
                Username = a.Username,
                DisplayName = a.DisplayName,
                ProfilePictureUrl = a.ProfilePictureUrl,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                LastSync = a.LastSync,
                FollowerCount = a.FollowerCount,
                FollowingCount = a.FollowingCount,
                MediaCount = a.MediaCount,
                TokenValid = a.TokenExpiresAt == null || a.TokenExpiresAt > DateTime.UtcNow,
                TokenExpiresAt = a.TokenExpiresAt
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpPost]
    public async Task<ActionResult<InstagramAccountResponse>> AddAccount(InstagramAccountRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            // Validar access token e obter informações do usuário
            var userInfo = await _instagramApiService.GetUserInfoAsync(request.AccessToken);
            if (userInfo == null)
            {
                return BadRequest(new { message = "Access token inválido ou expirado" });
            }

            // Verificar se a conta já existe
            var existingAccount = await _context.InstagramAccounts
                .FirstOrDefaultAsync(a => a.InstagramUserId == userInfo.Id);

            if (existingAccount != null)
            {
                if (existingAccount.UserId != userId)
                {
                    return Conflict(new { message = "Esta conta Instagram já está vinculada a outro usuário" });
                }

                // Atualizar conta existente
                existingAccount.AccessToken = request.AccessToken;
                existingAccount.Username = userInfo.Username;
                existingAccount.DisplayName = request.DisplayName ?? userInfo.Username;
                existingAccount.FollowerCount = userInfo.FollowersCount;
                existingAccount.FollowingCount = userInfo.FollowsCount;
                existingAccount.MediaCount = userInfo.MediaCount;
                existingAccount.LastSync = DateTime.UtcNow;
                existingAccount.UpdatedAt = DateTime.UtcNow;
                existingAccount.IsActive = true;

                await _context.SaveChangesAsync();

                var updatedResponse = new InstagramAccountResponse
                {
                    Id = existingAccount.Id,
                    InstagramUserId = existingAccount.InstagramUserId,
                    Username = existingAccount.Username,
                    DisplayName = existingAccount.DisplayName,
                    ProfilePictureUrl = existingAccount.ProfilePictureUrl,
                    IsActive = existingAccount.IsActive,
                    CreatedAt = existingAccount.CreatedAt,
                    LastSync = existingAccount.LastSync,
                    FollowerCount = existingAccount.FollowerCount,
                    FollowingCount = existingAccount.FollowingCount,
                    MediaCount = existingAccount.MediaCount,
                    TokenValid = true,
                    TokenExpiresAt = existingAccount.TokenExpiresAt
                };

                return Ok(updatedResponse);
            }

            // Criar nova conta
            var newAccount = new InstagramAccount
            {
                UserId = userId.Value,
                InstagramUserId = userInfo.Id,
                Username = userInfo.Username,
                DisplayName = request.DisplayName ?? userInfo.Username,
                AccessToken = request.AccessToken,
                FollowerCount = userInfo.FollowersCount,
                FollowingCount = userInfo.FollowsCount,
                MediaCount = userInfo.MediaCount,
                LastSync = DateTime.UtcNow,
                IsActive = true
            };

            _context.InstagramAccounts.Add(newAccount);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nova conta Instagram adicionada: {Username} para usuário {UserId}", userInfo.Username, userId);

            var response = new InstagramAccountResponse
            {
                Id = newAccount.Id,
                InstagramUserId = newAccount.InstagramUserId,
                Username = newAccount.Username,
                DisplayName = newAccount.DisplayName,
                ProfilePictureUrl = newAccount.ProfilePictureUrl,
                IsActive = newAccount.IsActive,
                CreatedAt = newAccount.CreatedAt,
                LastSync = newAccount.LastSync,
                FollowerCount = newAccount.FollowerCount,
                FollowingCount = newAccount.FollowingCount,
                MediaCount = newAccount.MediaCount,
                TokenValid = true,
                TokenExpiresAt = newAccount.TokenExpiresAt
            };

            return CreatedAtAction(nameof(GetAccount), new { id = newAccount.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar conta Instagram para usuário {UserId}", userId);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstagramAccountResponse>> GetAccount(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var account = await _context.InstagramAccounts
            .Where(a => a.Id == id && a.UserId == userId)
            .Select(a => new InstagramAccountResponse
            {
                Id = a.Id,
                InstagramUserId = a.InstagramUserId,
                Username = a.Username,
                DisplayName = a.DisplayName,
                ProfilePictureUrl = a.ProfilePictureUrl,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                LastSync = a.LastSync,
                FollowerCount = a.FollowerCount,
                FollowingCount = a.FollowingCount,
                MediaCount = a.MediaCount,
                TokenValid = a.TokenExpiresAt == null || a.TokenExpiresAt > DateTime.UtcNow,
                TokenExpiresAt = a.TokenExpiresAt
            })
            .FirstOrDefaultAsync();

        if (account == null)
        {
            return NotFound();
        }

        return Ok(account);
    }

    [HttpPost("{id}/sync")]
    public async Task<ActionResult<InstagramAccountResponse>> SyncAccount(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var account = await _context.InstagramAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(account.AccessToken))
        {
            return BadRequest(new { message = "Access token não disponível para esta conta" });
        }

        try
        {
            var userInfo = await _instagramApiService.GetUserInfoAsync(account.AccessToken);
            if (userInfo == null)
            {
                account.IsActive = false;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "Access token inválido ou expirado" });
            }

            // Atualizar informações da conta
            account.Username = userInfo.Username;
            account.FollowerCount = userInfo.FollowersCount;
            account.FollowingCount = userInfo.FollowsCount;
            account.MediaCount = userInfo.MediaCount;
            account.LastSync = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            account.IsActive = true;

            await _context.SaveChangesAsync();

            var response = new InstagramAccountResponse
            {
                Id = account.Id,
                InstagramUserId = account.InstagramUserId,
                Username = account.Username,
                DisplayName = account.DisplayName,
                ProfilePictureUrl = account.ProfilePictureUrl,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                LastSync = account.LastSync,
                FollowerCount = account.FollowerCount,
                FollowingCount = account.FollowingCount,
                MediaCount = account.MediaCount,
                TokenValid = true,
                TokenExpiresAt = account.TokenExpiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar conta Instagram {AccountId}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var account = await _context.InstagramAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound();
        }

        _context.InstagramAccounts.Remove(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Conta Instagram removida: {AccountId} do usuário {UserId}", id, userId);

        return NoContent();
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

