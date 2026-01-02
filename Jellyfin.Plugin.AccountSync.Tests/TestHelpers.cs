using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AccountSync.Tests;

public static class TestHelpers
{
    public static User CreateTestUser(string username, Guid? id = null)
    {
        var userId = id ?? Guid.NewGuid();
        var user = new User(username, "Jellyfin.Plugin.AccountSync.Tests", "Test");
        user.Id = userId;
        return user;
    }

    public static UserItemData CreateUserItemData(
        long playbackPosition = 0,
        bool played = false,
        int playCount = 0,
        DateTime? lastPlayedDate = null,
        int? audioStreamIndex = null,
        int? subtitleStreamIndex = null)
    {
        var userData = new UserItemData
        {
            Key = Guid.NewGuid().ToString(),
            PlaybackPositionTicks = playbackPosition,
            Played = played,
            PlayCount = playCount,
            LastPlayedDate = lastPlayedDate,
            AudioStreamIndex = audioStreamIndex,
            SubtitleStreamIndex = subtitleStreamIndex
        };
        return userData;
    }
}
