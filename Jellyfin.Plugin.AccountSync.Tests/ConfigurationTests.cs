using Jellyfin.Plugin.AccountSync.Configuration;

namespace Jellyfin.Plugin.AccountSync.Tests;

public class ConfigurationTests
{
    [Fact]
    public void AccountSyncPluginConfiguration_DefaultConstructor_InitializesEmptyList()
    {
        var config = new AccountSyncPluginConfiguration();

        Assert.NotNull(config.SyncList);
        Assert.Empty(config.SyncList);
    }

    [Fact]
    public void AddSyncAccount_AddsToList()
    {
        var config = new AccountSyncPluginConfiguration();
        var syncAccount = new Configuration.AccountSyncDto
        {
            SyncFromAccount = Guid.NewGuid(),
            SyncToAccount = Guid.NewGuid()
        };

        config.AddSyncAccount(syncAccount);

        Assert.Single(config.SyncList);
        Assert.Contains(syncAccount, config.SyncList);
    }

    [Fact]
    public void RemoveSyncAccount_RemovesFromList()
    {
        var config = new AccountSyncPluginConfiguration();
        var syncAccount = new Configuration.AccountSyncDto
        {
            SyncFromAccount = Guid.NewGuid(),
            SyncToAccount = Guid.NewGuid()
        };

        config.AddSyncAccount(syncAccount);
        config.RemoveSyncAccount(syncAccount);

        Assert.Empty(config.SyncList);
    }

    [Fact]
    public void AccountSync_PropertiesSetCorrectly()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        var syncAccount = new Configuration.AccountSyncDto
        {
            SyncFromAccount = fromId,
            SyncToAccount = toId
        };

        Assert.Equal(fromId, syncAccount.SyncFromAccount);
        Assert.Equal(toId, syncAccount.SyncToAccount);
    }

    [Fact]
    public void AddSyncAccount_WithSelfSync_ThrowsArgumentException()
    {
        var config = new AccountSyncPluginConfiguration();
        var userId = Guid.NewGuid();
        var syncAccount = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userId,
            SyncToAccount = userId
        };

        var exception = Assert.Throws<ArgumentException>(() => config.AddSyncAccount(syncAccount));
        Assert.Contains("cannot sync to itself", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSyncAccount_WithCircularSync_ThrowsInvalidOperationException()
    {
        var config = new AccountSyncPluginConfiguration();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        var syncAtoB = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userAId,
            SyncToAccount = userBId
        };

        var syncBtoA = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userBId,
            SyncToAccount = userAId
        };

        config.AddSyncAccount(syncAtoB);

        var exception = Assert.Throws<InvalidOperationException>(() => config.AddSyncAccount(syncBtoA));
        Assert.Contains("circular", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSyncAccount_WithDuplicateSync_ThrowsInvalidOperationException()
    {
        var config = new AccountSyncPluginConfiguration();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        var syncAccount1 = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userAId,
            SyncToAccount = userBId
        };

        var syncAccount2 = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userAId,
            SyncToAccount = userBId
        };

        config.AddSyncAccount(syncAccount1);

        var exception = Assert.Throws<InvalidOperationException>(() => config.AddSyncAccount(syncAccount2));
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSyncAccount_WithIndirectCircularSync_ThrowsInvalidOperationException()
    {
        var config = new AccountSyncPluginConfiguration();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var userCId = Guid.NewGuid();

        config.AddSyncAccount(new Configuration.AccountSyncDto { SyncFromAccount = userAId, SyncToAccount = userBId });
        config.AddSyncAccount(new Configuration.AccountSyncDto { SyncFromAccount = userBId, SyncToAccount = userCId });

        var syncCtoA = new Configuration.AccountSyncDto
        {
            SyncFromAccount = userCId,
            SyncToAccount = userAId
        };

        var exception = Assert.Throws<InvalidOperationException>(() => config.AddSyncAccount(syncCtoA));
        Assert.Contains("circular", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
