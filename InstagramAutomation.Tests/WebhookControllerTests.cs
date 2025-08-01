using System.Collections.Generic;
using System.Linq;
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
        var controller = new WebhookController(config, context, new StubInstagramApiService(), NullLogger<WebhookController>.Instance);

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
    }

    private class StubInstagramApiService : IInstagramApiService
    {
        public Task<InstagramUserInfo?> GetUserInfoAsync(string accessToken) => Task.FromResult<InstagramUserInfo?>(null);
        public Task<bool> ValidateAccessTokenAsync(string accessToken) => Task.FromResult(true);
        public Task<bool> PostCommentReplyAsync(string accessToken, string commentId, string message) => Task.FromResult(true);
        public Task<bool> SendPrivateMessageAsync(string accessToken, string userId, string message) => Task.FromResult(true);
        public Task<List<InstagramMedia>> GetUserMediaAsync(string accessToken, int limit = 10) => Task.FromResult(new List<InstagramMedia>());
    }
}
