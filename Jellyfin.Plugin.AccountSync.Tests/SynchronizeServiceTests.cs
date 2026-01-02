using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.AccountSync.Tests;

public class SynchronizeServiceTests
{
    private readonly Mock<IUserDataManager> _userDataManagerMock;
    private readonly Mock<ILogger<SynchronizeService>> _loggerMock;

    public SynchronizeServiceTests()
    {
        _userDataManagerMock = new Mock<IUserDataManager>();
        _loggerMock = new Mock<ILogger<SynchronizeService>>();
    }

    [Fact]
    public void Constructor_InitializesSuccessfully()
    {
        var service = new SynchronizeService(
            _userDataManagerMock.Object,
            _loggerMock.Object
        );

        Assert.NotNull(service);
    }

    [Fact]
    public void SynchronizeService_IsAccessibleViaInterface()
    {
        ISynchronizeService service = new SynchronizeService(
            _userDataManagerMock.Object,
            _loggerMock.Object
        );

        Assert.NotNull(service);
        Assert.IsAssignableFrom<ISynchronizeService>(service);
    }

    [Fact]
    public void SynchronizePlayState_WithPlayedToCompletion_UpdatesUserData()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        var userData = new MediaBrowser.Controller.Entities.UserItemData { Key = Guid.NewGuid().ToString(), PlaybackPositionTicks = 5000, Played = false, PlayCount = 0 };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(userData);

        service.SynchronizePlayState(syncToUser, item, 10000, true, CancellationToken.None);

        Assert.True(userData.Played);
        Assert.Equal(0, userData.PlaybackPositionTicks);
        Assert.Equal(1, userData.PlayCount);
        Assert.NotNull(userData.LastPlayedDate);
        _userDataManagerMock.Verify(x => x.SaveUserData(syncToUser, item, userData, MediaBrowser.Model.Entities.UserDataSaveReason.PlaybackProgress, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void SynchronizePlayState_WithPartialProgress_UpdatesPosition()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        var userData = new MediaBrowser.Controller.Entities.UserItemData { Key = Guid.NewGuid().ToString(), PlaybackPositionTicks = 0, Played = false, PlayCount = 0 };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(userData);

        service.SynchronizePlayState(syncToUser, item, 7500, false, CancellationToken.None);

        Assert.False(userData.Played);
        Assert.Equal(7500, userData.PlaybackPositionTicks);
        Assert.Equal(0, userData.PlayCount);
        _userDataManagerMock.Verify(x => x.SaveUserData(It.IsAny<Jellyfin.Database.Implementations.Entities.User>(), It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(), It.IsAny<MediaBrowser.Controller.Entities.UserItemData>(), MediaBrowser.Model.Entities.UserDataSaveReason.PlaybackProgress, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void SynchronizePlayState_WithNullUserData_DoesNothing()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns((MediaBrowser.Controller.Entities.UserItemData?)null);

        service.SynchronizePlayState(syncToUser, item, 10000, true, CancellationToken.None);

        _userDataManagerMock.Verify(x => x.SaveUserData(It.IsAny<Jellyfin.Database.Implementations.Entities.User>(), It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(), It.IsAny<MediaBrowser.Controller.Entities.UserItemData>(), It.IsAny<MediaBrowser.Model.Entities.UserDataSaveReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SynchronizeItemState_WithNewerData_SyncsSuccessfully()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 1000,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-2),
            PlayCount = 0,
            AudioStreamIndex = null,
            SubtitleStreamIndex = null
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-1),
            PlayCount = 3,
            AudioStreamIndex = 1,
            SubtitleStreamIndex = 2
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        Assert.Equal(5000, toUserData.PlaybackPositionTicks);
        Assert.False(toUserData.Played);
        Assert.Equal(0, toUserData.PlayCount);
        Assert.Equal(fromUserData.LastPlayedDate, toUserData.LastPlayedDate);
        Assert.Equal(1, toUserData.AudioStreamIndex);
        Assert.Equal(2, toUserData.SubtitleStreamIndex);
        _userDataManagerMock.Verify(x => x.SaveUserData(syncToUser, item, toUserData, MediaBrowser.Model.Entities.UserDataSaveReason.PlaybackProgress, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void SynchronizeItemState_WithOlderData_DoesNotSync()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = DateTime.Now
        };

var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            LastPlayedDate = DateTime.Now.AddDays(-1)
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        _userDataManagerMock.Verify(x => x.SaveUserData(It.IsAny<Jellyfin.Database.Implementations.Entities.User>(), It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(), It.IsAny<MediaBrowser.Controller.Entities.UserItemData>(), It.IsAny<MediaBrowser.Model.Entities.UserDataSaveReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SynchronizeItemState_WhenFromUserPlayed_ResetsPosition()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-2),
            PlayCount = 0
        };

var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 10000,
            Played = true,
            LastPlayedDate = DateTime.Now.AddDays(-1),
            PlayCount = 1
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        Assert.Equal(0, toUserData.PlaybackPositionTicks);
        Assert.True(toUserData.Played);
        Assert.Equal(1, toUserData.PlayCount);
        _userDataManagerMock.Verify(x => x.SaveUserData(syncToUser, item, toUserData, MediaBrowser.Model.Entities.UserDataSaveReason.PlaybackProgress, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void SynchronizeItemState_IncrementsPlayCount_OnlyWhenPlayed()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-2),
            PlayCount = 2
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-1),
            PlayCount = 3
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        Assert.Equal(2, toUserData.PlayCount);
        Assert.False(toUserData.Played);
        Assert.Equal(5000, toUserData.PlaybackPositionTicks);
    }

    [Fact]
    public void SynchronizePlayState_IncrementsPlayCount_OnlyWhenPlayedToCompletion()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        var userData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = false,
            PlayCount = 5
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(userData);

        service.SynchronizePlayState(syncToUser, item, 3000, false, CancellationToken.None);

        Assert.Equal(5, userData.PlayCount);
        Assert.False(userData.Played);
        Assert.Equal(3000, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void SynchronizePlayState_MultipleSyncs_DoesNotIncrementPlayCountMultipleTimes()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        var userData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = false,
            PlayCount = 0
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(userData);

        service.SynchronizePlayState(syncToUser, item, 1000, false, CancellationToken.None);
        service.SynchronizePlayState(syncToUser, item, 2000, false, CancellationToken.None);
        service.SynchronizePlayState(syncToUser, item, 3000, false, CancellationToken.None);

        Assert.Equal(0, userData.PlayCount);
        Assert.False(userData.Played);
    }

    [Fact]
    public void SynchronizeItemState_WhenToUserAlreadyPlayed_DoesNotIncrementPlayCount()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = true,
            LastPlayedDate = DateTime.Now.AddDays(-2),
            PlayCount = 3
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = true,
            LastPlayedDate = DateTime.Now.AddDays(-1),
            PlayCount = 1
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        Assert.Equal(3, toUserData.PlayCount);
        Assert.True(toUserData.Played);
    }

    [Fact]
    public void SynchronizeItemState_WhenTransitioningToPlayed_IncrementsPlayCount()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = DateTime.Now.AddDays(-2),
            PlayCount = 0
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = true,
            LastPlayedDate = DateTime.Now.AddDays(-1),
            PlayCount = 1
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        Assert.Equal(1, toUserData.PlayCount);
        Assert.True(toUserData.Played);
        Assert.Equal(0, toUserData.PlaybackPositionTicks);
    }

    [Fact]
    public void SynchronizePlayState_WhenAlreadyPlayed_DoesNotIncrementPlayCountAgain()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("TestUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };
        var userData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 0,
            Played = true,
            PlayCount = 1
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(userData);

        service.SynchronizePlayState(syncToUser, item, 0, true, CancellationToken.None);

        Assert.Equal(1, userData.PlayCount);
        Assert.True(userData.Played);
    }

    [Fact]
    public void SynchronizeItemState_WithNullFromUserLastPlayedDate_DoesNotSync()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 1000,
            Played = false,
            LastPlayedDate = DateTime.Now,
            PlayCount = 0
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = true,
            LastPlayedDate = null,
            PlayCount = 1
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        _userDataManagerMock.Verify(x => x.SaveUserData(It.IsAny<Jellyfin.Database.Implementations.Entities.User>(), It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(), It.IsAny<MediaBrowser.Controller.Entities.UserItemData>(), It.IsAny<MediaBrowser.Model.Entities.UserDataSaveReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SynchronizeItemState_WithBothNullLastPlayedDate_DoesNotSync()
    {
        var service = new SynchronizeService(_userDataManagerMock.Object, _loggerMock.Object);

        var syncToUser = new Jellyfin.Database.Implementations.Entities.User("ToUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var syncFromUser = new Jellyfin.Database.Implementations.Entities.User("FromUser", "Test", "TestProvider") { Id = Guid.NewGuid() };
        var item = new MediaBrowser.Controller.Entities.Movies.Movie { Id = Guid.NewGuid() };

        var toUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 1000,
            Played = false,
            LastPlayedDate = null,
            PlayCount = 0
        };

        var fromUserData = new MediaBrowser.Controller.Entities.UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = 5000,
            Played = false,
            LastPlayedDate = null,
            PlayCount = 0
        };

        _userDataManagerMock.Setup(x => x.GetUserData(syncToUser, item)).Returns(toUserData);
        _userDataManagerMock.Setup(x => x.GetUserData(syncFromUser, item)).Returns(fromUserData);

        service.SynchronizeItemState(syncToUser, syncFromUser, item, CancellationToken.None);

        _userDataManagerMock.Verify(x => x.SaveUserData(It.IsAny<Jellyfin.Database.Implementations.Entities.User>(), It.IsAny<MediaBrowser.Controller.Entities.BaseItem>(), It.IsAny<MediaBrowser.Controller.Entities.UserItemData>(), It.IsAny<MediaBrowser.Model.Entities.UserDataSaveReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
