using Discord;
using Discord.WebSocket;

namespace Fidobot.Utilities;

public static class DiscordHelper {
  public static readonly DiscordSocketClient client = new();

  public static async Task<IGuildChannel?> GetChannel(ulong guildID, ulong channelID) {
    IGuild guild = client.GetGuild(guildID);
    if (guild == null) {
      return null;
    }

    IGuildChannel channel = await guild.GetChannelAsync(channelID);
    if (channel == null) {
      return null;
    }

    return channel;
  }
}
