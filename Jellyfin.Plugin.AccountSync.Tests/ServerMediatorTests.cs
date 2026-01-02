using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.AccountSync.Tests;

public class ServerMediatorTests
{
    private readonly Mock<ISessionManager> _sessionManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IUserDataManager> _userDataManagerMock;
    private readonly Mock<ILogger<ServerMediator>> _loggerMock;
    private readonly Mock<ISynchronizeService> _syncServiceMock;

    public ServerMediatorTests()
    {
        _sessionManagerMock = new Mock<ISessionManager>();
        _userManagerMock = new Mock<IUserManager>();
        _userDataManagerMock = new Mock<IUserDataManager>();
        _loggerMock = new Mock<ILogger<ServerMediator>>();
        _syncServiceMock = new Mock<ISynchronizeService>();
    }

    [Fact]
    public void Constructor_InitializesSuccessfully()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        Assert.NotNull(mediator);
    }

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        await mediator.StartAsync(CancellationToken.None);
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        await mediator.StopAsync(CancellationToken.None);
        Assert.True(true);
    }

    [Fact]
    public async Task StartAsync_RegistersEventHandlers()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        await mediator.StartAsync(CancellationToken.None);

        _sessionManagerMock.VerifyAdd(x => x.PlaybackStopped += It.IsAny<EventHandler<PlaybackStopEventArgs>>(), Times.Once);
        _userDataManagerMock.VerifyAdd(x => x.UserDataSaved += It.IsAny<EventHandler<UserDataSaveEventArgs>>(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_UnregistersEventHandlers()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        await mediator.StartAsync(CancellationToken.None);
        await mediator.StopAsync(CancellationToken.None);

        _sessionManagerMock.VerifyRemove(x => x.PlaybackStopped -= It.IsAny<EventHandler<PlaybackStopEventArgs>>(), Times.Once);
        _userDataManagerMock.VerifyRemove(x => x.UserDataSaved -= It.IsAny<EventHandler<UserDataSaveEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_UnregistersEventHandlers()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        mediator.Dispose();

        _sessionManagerMock.VerifyRemove(x => x.PlaybackStopped -= It.IsAny<EventHandler<PlaybackStopEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_UnregistersUserDataSavedHandler()
    {
        var mediator = new ServerMediator(
            _sessionManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _loggerMock.Object,
            _syncServiceMock.Object
        );

        mediator.Dispose();

        _userDataManagerMock.VerifyRemove(x => x.UserDataSaved -= It.IsAny<EventHandler<UserDataSaveEventArgs>>(), Times.Once);
    }
}
