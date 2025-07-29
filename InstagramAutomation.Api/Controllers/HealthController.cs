using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstagramAutomation.Api.Data;

namespace InstagramAutomation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Verificar conexão com banco de dados
            await _context.Database.CanConnectAsync();

            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                database = "connected",
                uptime = Environment.TickCount64
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            var health = new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                database = "disconnected",
                error = ex.Message,
                uptime = Environment.TickCount64
            };

            return StatusCode(503, health);
        }
    }

    [HttpGet("database")]
    public async Task<IActionResult> DatabaseHealth()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var userCount = await _context.Users.CountAsync();
            var accountCount = await _context.InstagramAccounts.CountAsync();
            var ruleCount = await _context.AutomationRules.CountAsync();

            var dbHealth = new
            {
                status = canConnect ? "healthy" : "unhealthy",
                connection = canConnect,
                statistics = new
                {
                    users = userCount,
                    instagram_accounts = accountCount,
                    automation_rules = ruleCount
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(dbHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            var dbHealth = new
            {
                status = "unhealthy",
                connection = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            };

            return StatusCode(503, dbHealth);
        }
    }
}

