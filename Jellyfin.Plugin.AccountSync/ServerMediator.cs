using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountSync;

public sealed class ServerMediator : IHostedService, IDisposable
{
    private readonly ILogger<ServerMediator> _logger;

    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ISynchronizeService _synchronizeService;
    private readonly ISessionManager _sessionManager;

    public ServerMediator(
        ISessionManager sessionManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILogger<ServerMediator> logger,
        ISynchronizeService synchronizeService)
    {
        _sessionManager = sessionManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;
        _synchronizeService = synchronizeService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Account Sync plugin and registering event handlers");

        _sessionManager.PlaybackStopped += SessionManager_PlaybackStopped;
        _userDataManager.UserDataSaved += UserDataManager_UserDataSaved;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Account Sync plugin and unregistering event handlers");

        _sessionManager.PlaybackStopped -= SessionManager_PlaybackStopped;
        _userDataManager.UserDataSaved -= UserDataManager_UserDataSaved;

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sessionManager.PlaybackStopped -= SessionManager_PlaybackStopped;
        }
    }

    private void UserDataManager_UserDataSaved(object? sender, UserDataSaveEventArgs userDataSaveEventArgs)
    {
        _logger.LogInformation("UserDataSaved event triggered by {UserId}", userDataSaveEventArgs.UserId);
        if (userDataSaveEventArgs.SaveReason != UserDataSaveReason.TogglePlayed)
        {
            return;
        }

        if (userDataSaveEventArgs.Item == null)
        {
            return;
        }

        if (AccountSyncPlugin.Instance is null)
        {
            return;
        }

        var accountSyncs = AccountSyncPlugin.Instance.AccountSyncPluginConfiguration.SyncList.Where(user => user.SyncFromAccount == userDataSaveEventArgs.UserId).ToList();
        _logger.LogInformation("Item played state toggled manually. Syncing from {UserId}", userDataSaveEventArgs.UserId);

        foreach (var syncToUser in accountSyncs.Select(sync => _userManager.GetUserById(sync.SyncToAccount)).OfType<User>())
        {
            _logger.LogInformation("Syncing from {UserId} to {@SyncToUsername}", userDataSaveEventArgs.UserId, syncToUser.Username);
            _synchronizeService.SynchronizePlayState(syncToUser, userDataSaveEventArgs.Item, userDataSaveEventArgs.UserData.PlaybackPositionTicks, userDataSaveEventArgs.UserData.Played, CancellationToken.None);
        }
    }

    private void SessionManager_PlaybackStopped(object? sender, PlaybackStopEventArgs playbackStopEventArgs)
    {
        _logger.LogInformation("Playback stopped. Syncing from {SessionUserName}", playbackStopEventArgs.Session.UserName);

        if (AccountSyncPlugin.Instance is null)
        {
            return;
        }

        var accountSyncs = AccountSyncPlugin.Instance.Configuration.SyncList.Where(user => user.SyncFromAccount == playbackStopEventArgs.Session.UserId).ToList();

        foreach (var syncToUser in accountSyncs.Select(sync => _userManager.GetUserById(sync.SyncToAccount)).OfType<User>())
        {
            _logger.LogInformation("Syncing from {SessionUserName} to {@SyncToUsername}", playbackStopEventArgs.Session.UserName, syncToUser.Username);
            _synchronizeService.SynchronizePlayState(syncToUser, playbackStopEventArgs.Item, playbackStopEventArgs.PlaybackPositionTicks, playbackStopEventArgs.PlayedToCompletion, CancellationToken.None);
        }
    }
}
