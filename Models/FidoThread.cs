using Discord;
using Fidobot.Models.DB;
using Fidobot.Utilities;

namespace Fidobot.Models;

public class FidoThread {
  public IThreadChannel Channel;
  public DateTime EatDT;

  private FidoThread(IThreadChannel channel, DateTime eatDT) {
    Channel = channel;
    EatDT = eatDT;
  }

  public static async Task<FidoThread?> CreateAsync(DBThread dbThread) {
    IGuildChannel? channel = await DiscordHelper.GetChannel(dbThread.GuildID, dbThread.ChannelID);
    if (channel == null) {
      return null;
    }

    if (channel is not IThreadChannel thread) {
      return null;
    }

    DateTime eatDT = DateTimeOffset.FromUnixTimeSeconds(dbThread.EatTimestamp).UtcDateTime;
    return new FidoThread(thread, eatDT);
  }
}
