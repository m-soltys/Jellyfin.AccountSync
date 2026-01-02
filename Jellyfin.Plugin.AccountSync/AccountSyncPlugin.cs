using System;
using System.Collections.Generic;
using Jellyfin.Plugin.AccountSync.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AccountSync;

public class AccountSyncPlugin : BasePlugin<AccountSyncPluginConfiguration>, IHasWebPages
{
    public static AccountSyncPlugin? Instance { get; private set; }

    public override Guid Id
        => new("4BE0C7F2-515C-4F10-89FE-EF81EE85ABD8");

    public override string Name
        => "Account Sync";

    public override string Description
        => "Sync watched status between two Jellyfin user account profiles";

    public AccountSyncPluginConfiguration AccountSyncPluginConfiguration
        => Configuration;

    public AccountSyncPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages()
        => new[]
        {
            new PluginPageInfo
            {
                Name = "AccountSyncPluginConfigurationPage",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.AccountSyncPluginConfigurationPage.html"
            },
            new PluginPageInfo
            {
                Name = "AccountSyncPluginConfigurationPageJS",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.AccountSyncPluginConfigurationPage.js"
            }
        };
}