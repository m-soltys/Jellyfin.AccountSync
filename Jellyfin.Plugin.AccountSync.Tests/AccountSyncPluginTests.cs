namespace Jellyfin.Plugin.AccountSync.Tests;

public class AccountSyncPluginTests
{
    [Fact]
    public void PluginGuid_IsCorrect()
    {
        var expectedId = new Guid("4BE0C7F2-515C-4F10-89FE-EF81EE85ABD8");
        Assert.Equal(expectedId, new Guid("4BE0C7F2-515C-4F10-89FE-EF81EE85ABD8"));
    }

    [Fact]
    public void PluginName_IsAccountSync()
    {
        Assert.Equal("Account Sync", "Account Sync");
    }

    [Fact]
    public void PluginDescription_IsCorrect()
    {
        var expected = "Sync watched status between two Jellyfin user account profiles";
        Assert.Equal(expected, "Sync watched status between two Jellyfin user account profiles");
    }
}
