using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using InstagramAutomation.Api.Models;
using InstagramAutomation.Api.Services;
using Xunit;

public class JwtServiceTests
{
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        var inMemorySettings = new Dictionary<string,string>
        {
            {"JwtSettings:SecretKey", "supersecretkeysupersecretkey123"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpirationMinutes", "60"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        _service = new JwtService(configuration);
    }

    [Fact]
    public void GenerateAndValidateToken_ReturnsClaims()
    {
        var user = new User { Id = 1, Email = "test@example.com", FullName = "Test" };
        var token = _service.GenerateAccessToken(user);
        var principal = _service.ValidateToken(token);
        Assert.NotNull(principal);
        Assert.Equal("1", principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void GetUserIdFromToken_ReturnsUserId()
    {
        var user = new User { Id = 2, Email = "test2@example.com", FullName = "Test2" };
        var token = _service.GenerateAccessToken(user);
        var userId = _service.GetUserIdFromToken(token);
        Assert.Equal(2, userId);
    }
}
