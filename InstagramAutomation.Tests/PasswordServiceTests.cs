using InstagramAutomation.Api.Services;
using Xunit;

public class PasswordServiceTests
{
    [Fact]
    public void HashAndVerifyPassword_Works()
    {
        var service = new PasswordService();
        var hash = service.HashPassword("StrongP@ss1");
        Assert.True(service.VerifyPassword("StrongP@ss1", hash));
    }

    [Fact]
    public void IsPasswordStrong_ValidatesRequirements()
    {
        var service = new PasswordService();
        Assert.True(service.IsPasswordStrong("Abc123!@#"));
        Assert.False(service.IsPasswordStrong("weak"));
    }
}
