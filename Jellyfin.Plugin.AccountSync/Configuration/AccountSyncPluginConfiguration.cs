using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AccountSync.Configuration;

public class AccountSyncPluginConfiguration : BasePluginConfiguration
{
    #pragma warning disable CA2227
    public Collection<AccountSyncDto> SyncList { get; set; } = new();
    #pragma warning restore CA2227

    public void AddSyncAccount(AccountSyncDto accountSyncDto)
    {
        ArgumentNullException.ThrowIfNull(accountSyncDto);

        if (accountSyncDto.SyncFromAccount == accountSyncDto.SyncToAccount)
        {
            throw new ArgumentException($"User {accountSyncDto.SyncFromAccount} cannot sync to itself.", nameof(accountSyncDto));
        }

        if (SyncList.Any(s => s.SyncFromAccount == accountSyncDto.SyncFromAccount && s.SyncToAccount == accountSyncDto.SyncToAccount))
        {
            throw new InvalidOperationException($"Sync from {accountSyncDto.SyncFromAccount} to {accountSyncDto.SyncToAccount} already exists.");
        }

        if (WouldCreateCircularDependency(accountSyncDto))
        {
            throw new InvalidOperationException($"Adding sync from {accountSyncDto.SyncFromAccount} to {accountSyncDto.SyncToAccount} would create a circular dependency.");
        }

        SyncList.Add(accountSyncDto);
    }

    public void RemoveSyncAccount(AccountSyncDto accountSyncDto)
        => SyncList.Remove(accountSyncDto);

    private bool WouldCreateCircularDependency(AccountSyncDto newSync)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();

        stack.Push(newSync.SyncToAccount);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current == newSync.SyncFromAccount)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var sync in SyncList.Where(s => s.SyncFromAccount == current))
            {
                stack.Push(sync.SyncToAccount);
            }
        }

        return false;
    }
}

public class AccountSyncDto
{
    public Guid SyncToAccount { get; set; }

    public Guid SyncFromAccount { get; set; }
}