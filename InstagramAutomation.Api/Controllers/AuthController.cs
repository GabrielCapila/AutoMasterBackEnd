using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstagramAutomation.Api.Data;
using InstagramAutomation.Api.DTOs;
using InstagramAutomation.Api.Models;
using InstagramAutomation.Api.Services;

namespace InstagramAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Verificar se o email já existe
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new { message = "Email já está em uso" });
            }

            // Validar força da senha
            if (!_passwordService.IsPasswordStrong(request.Password))
            {
                return BadRequest(new { message = "Senha deve conter pelo menos 8 caracteres, incluindo maiúscula, minúscula, número e caractere especial" });
            }

            // Criar novo usuário
            var user = new User
            {
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = _passwordService.HashPassword(request.Password),
                FullName = request.full_name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário registrado com sucesso: {Email}", user.Email);

            // Gerar tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    SubscriptionTier = user.SubscriptionTier,
                    SubscriptionExpiresAt = user.SubscriptionExpiresAt,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário: {Email}", request.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Email ou senha inválidos" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Conta desativada" });
            }

            // Atualizar último login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login realizado com sucesso: {Email}", user.Email);

            // Gerar tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    SubscriptionTier = user.SubscriptionTier,
                    SubscriptionExpiresAt = user.SubscriptionExpiresAt,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer login: {Email}", request.Email);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
    {
        // Implementação simplificada - em produção, armazenar refresh tokens no banco
        return BadRequest(new { message = "Refresh token inválido" });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userInfo = new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            SubscriptionTier = user.SubscriptionTier,
            SubscriptionExpiresAt = user.SubscriptionExpiresAt,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };

        return Ok(userInfo);
    }
}

