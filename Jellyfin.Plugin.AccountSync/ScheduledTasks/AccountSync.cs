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

public class AccountSync : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISynchronizeService _synchronizeService;
    private readonly IUserManager _userManager;
    private readonly ILogger<AccountSync> _logger;

    public AccountSync(
        IUserManager userManager,
        ILibraryManager libraryManager,
        ISynchronizeService synchronizeService,
        ILogger<AccountSync> logger)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _synchronizeService = synchronizeService;
        _logger = logger;
    }

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
            _logger.LogWarning("AccountSyncPlugin.Instance is null. Cannot execute sync.");
            return Task.CompletedTask;
        }

        try
        {
            var currentProgress = 0.0;
            var progressPerUser = 100.0 / AccountSyncPlugin.Instance.Configuration.SyncList.Count;
            foreach (var syncProfile in AccountSyncPlugin.Instance.Configuration.SyncList)
            {
                var syncToUser = _userManager.GetUserById(syncProfile.SyncToAccount);
                var syncFromUser = _userManager.GetUserById(syncProfile.SyncFromAccount);

                if (syncToUser is null || syncFromUser is null)
                {
                    _logger.LogWarning("Could not find sync users. SyncTo: {SyncTo}, SyncFrom: {SyncFrom}", syncProfile.SyncToAccount, syncProfile.SyncFromAccount);
                    continue;
                }

                var queryItems = _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode } });

                var progressPerItem = progressPerUser / queryItems.Count;
                foreach (var item in queryItems)
                {
                    _synchronizeService.SynchronizeItemState(syncToUser, syncFromUser, item, cancellationToken);

                    currentProgress += progressPerItem;
                    progress.Report(currentProgress);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AccountSync scheduled task");
        }

        progress.Report(100.0);

        return Task.CompletedTask;
    }
}
