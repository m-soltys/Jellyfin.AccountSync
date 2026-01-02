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

internal sealed partial class SynchronizeService : ISynchronizeService
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

        LogFromItemDataSyncfromitemdata(syncFromItemData.PropertiesToString());
        LogToItemDataSynctoitemdata(syncToItemData.PropertiesToString());

        syncToItemData.PlaybackPositionTicks = syncFromItemData.Played ? 0 : syncFromItemData.PlaybackPositionTicks;
        syncToItemData.Played = syncFromItemData.Played;
        if (syncFromItemData.Played)
        {
            syncToItemData.PlayCount += 1;
        }

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

        LogPlayedPositionPlaybackpositionticksPlayedToCompletionPlayedtocompletion(playbackPositionTicks, playedToCompletion);
        LogToItemDataSynctouseritemdata(syncToUserItemData.PropertiesToString());

        var now = DateTime.Now;

        syncToUserItemData.PlaybackPositionTicks = playedToCompletion ? 0 : playbackPositionTicks ?? 0;

        var wasNotPlayedBefore = !syncToUserItemData.Played;
        syncToUserItemData.Played = playedToCompletion;
        if (playedToCompletion && wasNotPlayedBefore)
        {
            syncToUserItemData.PlayCount++;
        }

        syncToUserItemData.LastPlayedDate = now;

        _userDataManager.SaveUserData(syncToUser, item, syncToUserItemData, UserDataSaveReason.PlaybackProgress, cancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "From item data: {SyncFromItemData}")]
    partial void LogFromItemDataSyncfromitemdata(string SyncFromItemData);

    [LoggerMessage(LogLevel.Information, "To item data: {SyncToItemData}")]
    partial void LogToItemDataSynctoitemdata(string SyncToItemData);

    [LoggerMessage(LogLevel.Information, "Played position: {PlaybackPositionTicks}, played to completion: {PlayedToCompletion}")]
    partial void LogPlayedPositionPlaybackpositionticksPlayedToCompletionPlayedtocompletion(long? PlaybackPositionTicks, bool PlayedToCompletion);

    [LoggerMessage(LogLevel.Information, "To item data: {SyncToUserItemData}")]
    partial void LogToItemDataSynctouseritemdata(string SyncToUserItemData);
}