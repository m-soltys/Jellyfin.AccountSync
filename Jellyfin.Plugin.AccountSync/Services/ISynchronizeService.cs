using System;
using System.Threading;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.AccountSync.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountSync.Services;

public interface ISynchronizeService
{
    void SynchronizeItemState(
        User syncToUser,
        User syncFromUser,
        BaseItem item,
        CancellationToken cancellationToken);

    void SynchronizePlayState(
        User syncToUser,
        BaseItem item,
        long? playbackPositionTicks,
        bool playedToCompletion,
        CancellationToken cancellationToken);
}

internal sealed class SynchronizeService : ISynchronizeService
{
    private readonly ILogger<SynchronizeService> _logger;
    private readonly IUserDataManager _userDataManager;

    public SynchronizeService(IUserDataManager userDataManager, ILogger<SynchronizeService> logger)
    {
        _logger = logger;
        _userDataManager = userDataManager;
    }

    public void SynchronizeItemState(
        User syncToUser,
        User syncFromUser,
        BaseItem item,
        CancellationToken cancellationToken)
    {
        var syncToItemData = _userDataManager.GetUserData(syncToUser, item);
        var syncFromItemData = _userDataManager.GetUserData(syncFromUser, item);

        if (syncToItemData is null || syncFromItemData is null)
        {
            return;
        }

        if ((syncToItemData.PlaybackPositionTicks == syncFromItemData.PlaybackPositionTicks && syncToItemData.Played == syncFromItemData.Played)
            || syncFromItemData.LastPlayedDate == null || syncFromItemData.LastPlayedDate <= syncToItemData.LastPlayedDate)
        {
            return;
        }

        _logger.LogInformation("From item data: {SyncFromItemData}", syncFromItemData.PropertiesToString());
        _logger.LogInformation("To item data: {SyncToItemData}", syncToItemData.PropertiesToString());

        syncToItemData.PlaybackPositionTicks = syncFromItemData.Played ? 0 : syncFromItemData.PlaybackPositionTicks;
        syncToItemData.Played = syncFromItemData.Played;
        syncToItemData.PlayCount += 1;
        syncToItemData.LastPlayedDate = syncFromItemData.LastPlayedDate;
        syncToItemData.AudioStreamIndex = syncFromItemData.AudioStreamIndex;
        syncToItemData.SubtitleStreamIndex = syncFromItemData.SubtitleStreamIndex;

        _userDataManager.SaveUserData(syncToUser, item, syncToItemData, UserDataSaveReason.PlaybackProgress, cancellationToken);
    }

    public void SynchronizePlayState(
        User syncToUser,
        BaseItem item,
        long? playbackPositionTicks,
        bool playedToCompletion,
        CancellationToken cancellationToken)
    {
        var syncToUserItemData = _userDataManager.GetUserData(syncToUser, item);

        if (syncToUserItemData is null)
        {
            return;
        }

        _logger.LogInformation("Played position: {PlaybackPositionTicks}, played to completion: {PlayedToCompletion}", playbackPositionTicks, playedToCompletion);
        _logger.LogInformation("To item data: {SyncToUserItemData}", syncToUserItemData.PropertiesToString());

        var now = DateTime.Now;

        syncToUserItemData.PlaybackPositionTicks = playedToCompletion ? 0 : playbackPositionTicks ?? 0;
        syncToUserItemData.Played = playedToCompletion;
        syncToUserItemData.PlayCount++;
        syncToUserItemData.LastPlayedDate = now;

        _userDataManager.SaveUserData(syncToUser, item, syncToUserItemData, UserDataSaveReason.PlaybackProgress, cancellationToken);
    }
}
