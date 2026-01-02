using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountSync;

public sealed partial class ServerMediator : IHostedService, IDisposable
{
    private readonly ILogger<ServerMediator> _logger;

    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ISynchronizeService _synchronizeService;
    private readonly ISessionManager _sessionManager;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _syncLocks = new();

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
        LogStartingAccountSyncPluginAndRegisteringEventHandlers();

        _sessionManager.PlaybackStopped += SessionManager_PlaybackStopped;
        _userDataManager.UserDataSaved += UserDataManager_UserDataSaved;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogStoppingAccountSyncPluginAndUnregisteringEventHandlers();

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
            _userDataManager.UserDataSaved -= UserDataManager_UserDataSaved;

            foreach (var semaphore in _syncLocks.Values)
            {
                semaphore.Dispose();
            }

            _syncLocks.Clear();
        }
    }

    private void UserDataManager_UserDataSaved(object? sender, UserDataSaveEventArgs userDataSaveEventArgs)
    {
        LogUserdatasavedEventTriggeredByUserid(userDataSaveEventArgs.UserId);
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
        LogItemPlayedStateToggledManuallySyncingFromUserid(userDataSaveEventArgs.UserId);

        foreach (var syncToUser in accountSyncs.Select(sync => _userManager.GetUserById(sync.SyncToAccount)).Where(u => u != null)!)
        {
            LogSyncingFromUseridToSynctousername(userDataSaveEventArgs.UserId, syncToUser.Username);

            var lockKey = $"{syncToUser.Id}:{userDataSaveEventArgs.Item.Id}";
            var semaphore = _syncLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            Task.Run(async () =>
            {
                if (await semaphore.WaitAsync(0).ConfigureAwait(false))
                {
                    try
                    {
                        _synchronizeService.SynchronizePlayState(syncToUser, userDataSaveEventArgs.Item, userDataSaveEventArgs.UserData.PlaybackPositionTicks, userDataSaveEventArgs.UserData.Played, CancellationToken.None);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            });
        }
    }

    private void SessionManager_PlaybackStopped(object? sender, PlaybackStopEventArgs playbackStopEventArgs)
    {
        LogPlaybackStoppedSyncingFromSessionusername(playbackStopEventArgs.Session.UserName);

        if (AccountSyncPlugin.Instance is null)
        {
            return;
        }

        var accountSyncs = AccountSyncPlugin.Instance.Configuration.SyncList.Where(user => user.SyncFromAccount == playbackStopEventArgs.Session.UserId).ToList();

        foreach (var syncToUser in accountSyncs.Select(sync => _userManager.GetUserById(sync.SyncToAccount)).Where(u => u != null)!)
        {
            LogSyncingFromSessionusernameToSynctousername(playbackStopEventArgs.Session.UserName, syncToUser.Username);

            var lockKey = $"{syncToUser.Id}:{playbackStopEventArgs.Item.Id}";
            var semaphore = _syncLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            Task.Run(async () =>
            {
                if (await semaphore.WaitAsync(0).ConfigureAwait(false))
                {
                    try
                    {
                        _synchronizeService.SynchronizePlayState(syncToUser, playbackStopEventArgs.Item, playbackStopEventArgs.PlaybackPositionTicks, playbackStopEventArgs.PlayedToCompletion, CancellationToken.None);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            });
        }
    }

    [LoggerMessage(LogLevel.Information, "Starting Account Sync plugin and registering event handlers")]
    partial void LogStartingAccountSyncPluginAndRegisteringEventHandlers();

    [LoggerMessage(LogLevel.Information, "Stopping Account Sync plugin and unregistering event handlers")]
    partial void LogStoppingAccountSyncPluginAndUnregisteringEventHandlers();

    [LoggerMessage(LogLevel.Information, "UserDataSaved event triggered by {UserId}")]
    partial void LogUserdatasavedEventTriggeredByUserid(Guid UserId);

    [LoggerMessage(LogLevel.Information, "Item played state toggled manually. Syncing from {UserId}")]
    partial void LogItemPlayedStateToggledManuallySyncingFromUserid(Guid UserId);

    [LoggerMessage(LogLevel.Information, "Syncing from {UserId} to {@SyncToUsername}")]
    partial void LogSyncingFromUseridToSynctousername(Guid UserId, string @SyncToUsername);

    [LoggerMessage(LogLevel.Information, "Playback stopped. Syncing from {SessionUserName}")]
    partial void LogPlaybackStoppedSyncingFromSessionusername(string SessionUserName);

    [LoggerMessage(LogLevel.Information, "Syncing from {SessionUserName} to {@SyncToUsername}")]
    partial void LogSyncingFromSessionusernameToSynctousername(string SessionUserName, string @SyncToUsername);
}