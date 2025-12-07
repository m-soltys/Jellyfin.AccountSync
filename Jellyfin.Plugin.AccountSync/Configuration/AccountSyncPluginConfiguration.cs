using System;
using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AccountSync.Configuration;

public class AccountSyncPluginConfiguration : BasePluginConfiguration
{
    public AccountSyncPluginConfiguration()
    {
        SyncList = new Collection<AccountSync>();
    }

#pragma warning disable CA2227
    public Collection<AccountSync> SyncList { get; set; }
#pragma warning restore CA2227

    public void AddSyncAccount(AccountSync accountSync)
    {
        SyncList.Add(accountSync);
    }

    public void RemoveSyncAccount(AccountSync accountSync)
    {
        SyncList.Remove(accountSync);
    }
}

public class AccountSync
{
    public Guid SyncToAccount { get; set; }

    public Guid SyncFromAccount { get; set; }
}
