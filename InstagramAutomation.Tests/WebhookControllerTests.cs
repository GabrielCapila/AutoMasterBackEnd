using System.Text.Json;
using InstagramAutomation.Api.Controllers;
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
}
