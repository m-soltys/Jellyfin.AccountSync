const AccountSyncConfigurationPage = {
    pluginUniqueId: "4BE0C7F2-515C-4F10-89FE-EF81EE85ABD8",

    async getSyncProfileUsersData(syncProfile) {
        const users = await ApiClient.getJSON(ApiClient.getUrl("Users"));
        return {
            toUser: users.find(user => user.Id === syncProfile.SyncToAccount),
            fromUser: users.find(user => user.Id === syncProfile.SyncFromAccount)
        };
    },

    createDeleteButton(fromUserId, toUserId, container) {
        const deleteButton = document.createElement('span');

        deleteButton.innerHTML = '<span class="material-icons close"></span>';
        deleteButton.className = 'fab emby-button btnDeleteProfile';
        Object.assign(deleteButton.style, {position: 'absolute', right: '2px', margin: '1em'});
        deleteButton.addEventListener('click', () => this.deleteSyncProfile(fromUserId, toUserId, container));

        return deleteButton;
    },

    getProfileHtml({ fromUser, toUser }) {
        const container = document.createElement('div');
        container.className = 'syncButtonContainer cardBox visualCardBox';
        Object.assign(container.style, { maxWidth: '322px', width: '322px', position: 'relative' });
        container.dataset.syncTo = toUser.Id;
        container.dataset.syncFrom = fromUser.Id;

        container.appendChild(this.createDeleteButton(fromUser.Id, toUser.Id, container));

        const fromUserElement = document.createElement('h3');
        fromUserElement.style.margin = '1em';
        fromUserElement.innerHTML = `<span class="material-icons person" style="vertical-align: middle;"></span> From: ${fromUser.Name}`;
        container.appendChild(fromUserElement);

        const toUserElement = document.createElement('h3');
        toUserElement.style.margin = '1em';
        toUserElement.innerHTML = `<span class="material-icons person" style="vertical-align: middle;"></span> To: ${toUser.Name}`;
        container.appendChild(toUserElement);

        return container;
    },

    async deleteSyncProfile(syncFrom, syncTo, container) {
        const config = await ApiClient.getPluginConfiguration(this.pluginUniqueId);
        config.SyncList = config.SyncList.filter(c => !(c.SyncToAccount === syncTo && c.SyncFromAccount === syncFrom));

        const result = await ApiClient.updatePluginConfiguration(this.pluginUniqueId, config);

        if(result.ok) {
            container.remove();
        }
        Dashboard.hideLoadingMsg();
        Dashboard.processPluginConfigurationUpdateResult(result);
    },

    updateUserSelectOptions(userSelect, users) {
        userSelect.innerHTML = users.map(user => `<option value="${user.Id}">${user.Name}</option>`).join('');
    },

    async populateProfiles(view) {
        const savedProfileCards = view.querySelector('#existingSyncProfilesContainer');
        savedProfileCards.innerHTML = "";
        const config = await ApiClient.getPluginConfiguration(this.pluginUniqueId);
        if (config.SyncList) {
            for (const profile of config.SyncList) {
                const result = await this.getSyncProfileUsersData(profile);
                savedProfileCards.appendChild(this.getProfileHtml(result));
            }
        }
    },

    addEventListeners(view) {
        view.querySelector('#addSyncProfile').addEventListener('click', () => this.handleAddSyncProfileClick(view));
        view.addEventListener('viewshow', () => this.onViewShow(view));
    },

    async onViewShow(view) {
        const userOneSelect = view.querySelector('#syncToAccount');
        const userTwoSelect = view.querySelector('#syncFromAccount');
        const users = await ApiClient.getJSON(ApiClient.getUrl("Users"));
        this.updateUserSelectOptions(userOneSelect, users);
        this.updateUserSelectOptions(userTwoSelect, users);
        await this.populateProfiles(view);
        Dashboard.hideLoadingMsg();
    },

    async handleAddSyncProfileClick(view) {
        Dashboard.showLoadingMsg();
        const userOneSelect = view.querySelector('#syncToAccount').value;
        const userTwoSelect = view.querySelector('#syncFromAccount').value;
        const config = await ApiClient.getPluginConfiguration(this.pluginUniqueId);

        const syncProfile = {SyncToAccount: userOneSelect, SyncFromAccount: userTwoSelect};
        if (!config.SyncList.some(sync => sync.SyncToAccount === syncProfile.SyncToAccount && sync.SyncFromAccount === syncProfile.SyncFromAccount)) {
            config.SyncList.push(syncProfile);
            const configurationUpdateResult = await ApiClient.updatePluginConfiguration(this.pluginUniqueId, config);
            await this.populateProfiles(view);
            Dashboard.processPluginConfigurationUpdateResult(configurationUpdateResult);
        }
        Dashboard.hideLoadingMsg();
    }
};

export default function (view) {
    Dashboard.showLoadingMsg();
    AccountSyncConfigurationPage.addEventListeners(view);
}
