using Discord;
using Discord.WebSocket;

namespace Fidobot.Utilities;

public static class DiscordHelper {
  public static readonly DiscordSocketClient client = new();

  public static IForumChannel GetParentForum(this IThreadChannel thread) {
    if (thread == null)
      throw new ArgumentNullException(nameof(thread));

    if (thread is not SocketThreadChannel socketThread)
      throw new ArgumentException("The channel is not a SocketThreadChannel.", nameof(thread));

    if (socketThread.ParentChannel is not SocketGuildChannel socketForum)
      throw new ArgumentException("The parent channel is not a SocketGuildChannel.", nameof(thread));

    return socketForum as IForumChannel ?? throw new ArgumentException("The parent channel is not a forum channel.", nameof(thread));
  }

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

  private static bool IsAdmin(SocketGuildUser user) => user.GuildPermissions.Administrator;

  public static bool CanManageThreads(SocketGuildUser user, IGuildChannel channel) {
    if (IsAdmin(user)) {
      return true;
    }

    return channel.GetChannelType() switch {
      ChannelType.Forum => user.GuildPermissions.ManageThreads,
      ChannelType.PublicThread => user.GetPermissions(channel).ManageThreads,
      _ => false
    };
  }

  public static bool CanManageThreads(SocketGuildUser user) {
    if (IsAdmin(user)) {
      return true;
    }
    return user.GuildPermissions.ManageThreads;
  }
}
