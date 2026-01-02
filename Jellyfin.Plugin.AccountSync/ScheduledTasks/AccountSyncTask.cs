using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.AccountSync.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AccountSync.ScheduledTasks;

public partial class AccountSyncTask(
    IUserManager userManager,
    ILibraryManager libraryManager,
    ISynchronizeService synchronizeService,
    ILogger<AccountSyncTask> logger)
    : IScheduledTask, IConfigurableScheduledTask
{
    public bool IsHidden
        => false;

    public bool IsEnabled
        => true;

    public bool IsLogged
        => true;

    public string Name
        => "Sync progress between accounts";

    public string Key
        => "Accounts Playback Sync";

    public string Description
        => "Sync watched states for media items between accounts.";

    public string Category
        => "Accounts Playback Sync";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(24).Ticks }
        };
    }

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (AccountSyncPlugin.Instance is null)
        {
            LogAccountsyncpluginInstanceIsNullCannotExecuteSync();
            return Task.CompletedTask;
        }

        try
        {
            if (AccountSyncPlugin.Instance.Configuration.SyncList.Count == 0)
            {
                progress.Report(100.0);
                return Task.CompletedTask;
            }

            var currentProgress = 0.0;
            var progressPerUser = 100.0 / AccountSyncPlugin.Instance.Configuration.SyncList.Count;
            foreach (var syncProfile in AccountSyncPlugin.Instance.Configuration.SyncList)
            {
                var syncToUser = userManager.GetUserById(syncProfile.SyncToAccount);
                var syncFromUser = userManager.GetUserById(syncProfile.SyncFromAccount);

                if (syncToUser is null || syncFromUser is null)
                {
                    LogCouldNotFindSyncUsersSynctoSynctoSyncfromSyncfrom(syncProfile.SyncToAccount, syncProfile.SyncFromAccount);
                    continue;
                }

                var queryItems = libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode } });

                if (queryItems == null || queryItems.Count == 0)
                {
                    currentProgress += progressPerUser;
                    progress.Report(currentProgress);
                    continue;
                }

                var progressPerItem = progressPerUser / queryItems.Count;
                foreach (var item in queryItems)
                {
                    synchronizeService.SynchronizeItemState(syncToUser, syncFromUser, item, cancellationToken);

                    currentProgress += progressPerItem;
                    progress.Report(currentProgress);
                }
            }
        }
        catch (Exception ex)
        {
            LogErrorDuringAccountsyncScheduledTask(ex);
            throw;
        }

        progress.Report(100.0);

        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Warning, "AccountSyncPlugin.Instance is null. Cannot execute sync.")]
    partial void LogAccountsyncpluginInstanceIsNullCannotExecuteSync();

    [LoggerMessage(LogLevel.Warning, "Could not find sync users. SyncTo: {SyncTo}, SyncFrom: {SyncFrom}")]
    partial void LogCouldNotFindSyncUsersSynctoSynctoSyncfromSyncfrom(Guid SyncTo, Guid SyncFrom);

    [LoggerMessage(LogLevel.Error, "Error during AccountSync scheduled task")]
    partial void LogErrorDuringAccountsyncScheduledTask(Exception exception);
}