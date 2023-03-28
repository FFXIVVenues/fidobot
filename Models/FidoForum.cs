using Discord;
using Fidobot.Models.DB;
using Fidobot.Utilities;

namespace Fidobot.Models;

public class FidoForum {
  public IForumChannel Channel;
  public DateTime Started;
  public bool EatExisting;
  public TimeSpan EatOffset;

  private FidoForum(IForumChannel channel, DateTime started, bool eatExisting, TimeSpan eatOffset) {
    Channel = channel;
    Started = started;
    EatExisting = eatExisting;
    EatOffset = eatOffset;
  }

  public static async Task<FidoForum?> CreateAsync(DBForum dbForum) {
    IGuildChannel? channel = await DiscordHelper.GetChannel(dbForum.GuildID, dbForum.ChannelID);
    if (channel is not IForumChannel forum) {
      return null;
    }

    DateTime started = DateTimeOffset.FromUnixTimeSeconds(dbForum.StartedTimestamp).UtcDateTime;
    TimeSpan eatOffset = TimeSpan.FromSeconds(dbForum.EatOffset);
    return new FidoForum(forum, started, dbForum.EatExisting, eatOffset);
  }
}
