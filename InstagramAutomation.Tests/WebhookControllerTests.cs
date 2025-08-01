using System.Collections.Generic;
using System.Linq;

using System.Text.Json;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using InstagramAutomation.Api.Controllers;
using InstagramAutomation.Api.Data;
using InstagramAutomation.Api.DTOs;
using InstagramAutomation.Api.Models;
using InstagramAutomation.Api.Services;
using Xunit;

public class WebhookControllerTests
{
    [Fact]

    public void ParseRequests_AcceptsSingleObject()
    {
        var json = "{" +
            "\"object\":\"page\"," +
            "\"entry\":[{\"id\":\"1\",\"time\":0,\"changes\":[]}]}";
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var result = WebhookController.ParseRequests(element);
        Assert.Single(result);
        Assert.Equal("page", result[0].Object);
    }

    [Fact]
    public void ParseRequests_AcceptsArray()
    {
        var json = "[{\"object\":\"page\",\"entry\":[{\"id\":\"1\",\"time\":0,\"changes\":[]}]},{\"object\":\"page\",\"entry\":[{\"id\":\"2\",\"time\":0,\"changes\":[]}]}]";
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var result = WebhookController.ParseRequests(element);
        Assert.Equal(2, result.Count);
    }

    [Fact]

    public async Task Receive_WithBusinessLoginPayload_StoresCommentEvent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "webhook_db")
            .Options;
        using var context = new ApplicationDbContext(options);

        context.InstagramAccounts.Add(new InstagramAccount
        {
            Id = 1,
            InstagramUserId = "123",
            Username = "testuser"
        });
        context.SaveChanges();

        var config = new ConfigurationBuilder().Build();
        var stubService = new StubInstagramApiService();
        var controller = new WebhookController(config, context, stubService, NullLogger<WebhookController>.Instance);

        var request = new WebhookRequest
        {
            Object = "instagram",
            Entry = new List<WebhookEntry>
            {
                new WebhookEntry
                {
                    Id = "123",
                    Time = 1710687834,
                    Field = "comments",
                    Value = new WebhookValue
                    {
                        Id = "c1",
                        Text = "Nice post",
                        From = new WebhookFrom{ Id = "u1", Username = "john" },
                        Media = new WebhookMedia{ Id = "m1", MediaType = "FEED" }
                    }
                }
            }
        };

        var result = await controller.Receive(request);
        Assert.IsType<OkResult>(result);
        var ev = context.CommentEvents.Single();
        Assert.Equal("c1", ev.CommentId);
        Assert.Equal("john", ev.CommenterUsername);
        Assert.Equal("Nice post", ev.CommentText);
        Assert.Equal("FEED", ev.MediaType);
        Assert.True(stubService.Replied);
        Assert.True(stubService.SentDm);
    }

    private class StubInstagramApiService : IInstagramApiService
    {
        public Task<InstagramUserInfo?> GetUserInfoAsync(string accessToken) => Task.FromResult<InstagramUserInfo?>(null);
        public Task<bool> ValidateAccessTokenAsync(string accessToken) => Task.FromResult(true);
        public bool Replied { get; private set; }
        public bool SentDm { get; private set; }
        public Task<bool> PostCommentReplyAsync(string accessToken, string commentId, string message) { Replied = true; return Task.FromResult(true); }
        public Task<bool> SendPrivateMessageAsync(string accessToken, string userId, string message) { SentDm = true; return Task.FromResult(true); }
        public Task<List<InstagramMedia>> GetUserMediaAsync(string accessToken, int limit = 10) => Task.FromResult(new List<InstagramMedia>());
    }
}
