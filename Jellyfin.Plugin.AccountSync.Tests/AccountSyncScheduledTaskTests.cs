using System.Reflection;
using Jellyfin.Plugin.AccountSync.ScheduledTasks;
using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.AccountSync.Tests;

public class AccountSyncScheduledTaskTests : IDisposable
{
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<ISynchronizeService> _syncServiceMock;
    private readonly Mock<ILogger<ScheduledTasks.AccountSyncTask>> _loggerMock;
    private readonly ScheduledTasks.AccountSyncTask _task;
    private readonly AccountSyncPlugin _plugin;

    public AccountSyncScheduledTaskTests()
    {
        _userManagerMock = new Mock<IUserManager>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _syncServiceMock = new Mock<ISynchronizeService>();
        _loggerMock = new Mock<ILogger<ScheduledTasks.AccountSyncTask>>();

        _task = new ScheduledTasks.AccountSyncTask(
            _userManagerMock.Object,
            _libraryManagerMock.Object,
            _syncServiceMock.Object,
            _loggerMock.Object
        );

        // Initialize plugin instance for integration tests
        var appPathsMock = new Mock<IApplicationPaths>();
        var tempPath = Path.GetTempPath();
        appPathsMock.Setup(x => x.PluginConfigurationsPath).Returns(tempPath);
        appPathsMock.Setup(x => x.PluginsPath).Returns(tempPath);
        appPathsMock.Setup(x => x.DataPath).Returns(tempPath);
        var xmlSerializerMock = new Mock<IXmlSerializer>();

        _plugin = new AccountSyncPlugin(appPathsMock.Object, xmlSerializerMock.Object);
    }

    public void Dispose()
    {
        // Clear plugin instance after each test
        var instanceField = typeof(AccountSyncPlugin).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Name_ReturnsSyncProgressBetweenAccounts()
    {
        Assert.Equal("Sync progress between accounts", _task.Name);
    }

    [Fact]
    public void Key_ReturnsCorrectKey()
    {
        Assert.Equal("Accounts Playback Sync", _task.Key);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_task.Description));
        Assert.Equal("Sync watched states for media items between accounts.", _task.Description);
    }

    [Fact]
    public void IsHidden_ReturnsFalse()
    {
        Assert.False(_task.IsHidden);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        Assert.True(_task.IsEnabled);
    }

    [Fact]
    public void IsLogged_ReturnsTrue()
    {
        Assert.True(_task.IsLogged);
    }

    [Fact]
    public void GetDefaultTriggers_ReturnsIntervalTrigger()
    {
        var triggers = _task.GetDefaultTriggers();
        Assert.NotNull(triggers);
        Assert.Single(triggers);
        var trigger = triggers.First();
        Assert.Equal(MediaBrowser.Model.Tasks.TaskTriggerInfoType.IntervalTrigger, trigger.Type);
        Assert.Equal(TimeSpan.FromHours(24).Ticks, trigger.IntervalTicks);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullConfiguration_CompletesSuccessfully()
    {
        var progress = new Mock<IProgress<double>>();
        _plugin.UpdateConfiguration(new Configuration.AccountSyncPluginConfiguration());

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConfiguration_CallsSynchronizeService()
    {
        var progress = new Mock<IProgress<double>>();
        var syncFromUserId = Guid.NewGuid();
        var syncToUserId = Guid.NewGuid();

        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = syncFromUserId };
        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = syncToUserId };

        _userManagerMock.Setup(x => x.GetUserById(syncFromUserId)).Returns(syncFromUser);
        _userManagerMock.Setup(x => x.GetUserById(syncToUserId)).Returns(syncToUser);

        var movie = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid(), Name = "Test Movie" };
        _libraryManagerMock.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
            .Returns(new List<MediaBrowser.Controller.Entities.BaseItem> { movie });

        var config = new Configuration.AccountSyncPluginConfiguration();
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = syncFromUserId, SyncToAccount = syncToUserId });
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        _syncServiceMock.Verify(x => x.SynchronizeItemState(
            It.Is<Jellyfin.Database.Implementations.Entities.User>(u => u.Id == syncToUserId),
            It.Is<Jellyfin.Database.Implementations.Entities.User>(u => u.Id == syncFromUserId),
            It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(),
            It.IsAny<CancellationToken>()), Times.Once);

        progress.Verify(x => x.Report(It.IsAny<double>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSyncProfiles_ProcessesAll()
    {
        var progress = new Mock<IProgress<double>>();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        var user1 = new Jellyfin.Database.Implementations.Entities.User("User1", "Test", "TestProvider") { Id = user1Id };
        var user2 = new Jellyfin.Database.Implementations.Entities.User("User2", "Test", "TestProvider") { Id = user2Id };
        var user3 = new Jellyfin.Database.Implementations.Entities.User("User3", "Test", "TestProvider") { Id = user3Id };

        _userManagerMock.Setup(x => x.GetUserById(user1Id)).Returns(user1);
        _userManagerMock.Setup(x => x.GetUserById(user2Id)).Returns(user2);
        _userManagerMock.Setup(x => x.GetUserById(user3Id)).Returns(user3);

        var movie = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        _libraryManagerMock.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
            .Returns(new List<MediaBrowser.Controller.Entities.BaseItem> { movie });

        var config = new Configuration.AccountSyncPluginConfiguration();
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = user1Id, SyncToAccount = user2Id });
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = user1Id, SyncToAccount = user3Id });
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        _syncServiceMock.Verify(x => x.SynchronizeItemState(
            It.IsAny<Jellyfin.Database.Implementations.Entities.User>(),
            It.IsAny<Jellyfin.Database.Implementations.Entities.User>(),
            It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingUser_SkipsSyncProfile()
    {
        var progress = new Mock<IProgress<double>>();
        var existingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();

        var existingUser = new Jellyfin.Database.Implementations.Entities.User("ExistingUser", "Test", "TestProvider") { Id = existingUserId };

        _userManagerMock.Setup(x => x.GetUserById(existingUserId)).Returns(existingUser);
        _userManagerMock.Setup(x => x.GetUserById(missingUserId)).Returns((Jellyfin.Database.Implementations.Entities.User?)null);

        _libraryManagerMock.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
            .Returns(new List<MediaBrowser.Controller.Entities.BaseItem>());

        var config = new Configuration.AccountSyncPluginConfiguration();
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = existingUserId, SyncToAccount = missingUserId });
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        _syncServiceMock.Verify(x => x.SynchronizeItemState(
            It.IsAny<Jellyfin.Database.Implementations.Entities.User>(),
            It.IsAny<Jellyfin.Database.Implementations.Entities.User>(),
            It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressCorrectly()
    {
        var progress = new Mock<IProgress<double>>();
        var progressReports = new List<double>();
        progress.Setup(x => x.Report(It.IsAny<double>())).Callback<double>(d => progressReports.Add(d));

        var syncFromUserId = Guid.NewGuid();
        var syncToUserId = Guid.NewGuid();
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = syncFromUserId };
        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = syncToUserId };

        _userManagerMock.Setup(x => x.GetUserById(syncFromUserId)).Returns(syncFromUser);
        _userManagerMock.Setup(x => x.GetUserById(syncToUserId)).Returns(syncToUser);

        var movie = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        _libraryManagerMock.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
            .Returns(new List<MediaBrowser.Controller.Entities.BaseItem> { movie });

        var config = new Configuration.AccountSyncPluginConfiguration();
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = syncFromUserId, SyncToAccount = syncToUserId });
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        Assert.NotEmpty(progressReports);
        Assert.Contains(100.0, progressReports);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptySyncList_DoesNotCauseDivisionByZero()
    {
        var progress = new Mock<IProgress<double>>();
        var config = new Configuration.AccountSyncPluginConfiguration();
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        progress.Verify(x => x.Report(100.0), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyLibrary_DoesNotCauseDivisionByZero()
    {
        var progress = new Mock<IProgress<double>>();
        var syncFromUserId = Guid.NewGuid();
        var syncToUserId = Guid.NewGuid();

        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = syncFromUserId };
        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = syncToUserId };

        _userManagerMock.Setup(x => x.GetUserById(syncFromUserId)).Returns(syncFromUser);
        _userManagerMock.Setup(x => x.GetUserById(syncToUserId)).Returns(syncToUser);

        _libraryManagerMock.Setup(x => x.GetItemList(It.IsAny<MediaBrowser.Controller.Entities.InternalItemsQuery>()))
            .Returns(new List<MediaBrowser.Controller.Entities.BaseItem>());

        var config = new Configuration.AccountSyncPluginConfiguration();
        config.SyncList.Add(new Configuration.AccountSyncDto { SyncFromAccount = syncFromUserId, SyncToAccount = syncToUserId });
        _plugin.UpdateConfiguration(config);

        await _task.ExecuteAsync(progress.Object, CancellationToken.None);

        progress.Verify(x => x.Report(100.0), Times.AtLeast(1));
    }
}
