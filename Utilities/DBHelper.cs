using Discord;
using Fidobot.Models;
using LiteDB;

namespace Fidobot.Utilities;

public static class DBHelper {
  private static readonly LiteDatabase db = new(@"./main.db");
  private static readonly ILiteCollection<ThreadConfig> threads = db.GetCollection<ThreadConfig>();
  private static readonly ILiteCollection<ForumConfig> forums = db.GetCollection<ForumConfig>();

  public static void SaveThreadToDB(ulong guildID, ulong channelID, DateTime eatAt) {
    ThreadConfig config = new() {
      GuildID = guildID,
      ChannelID = channelID,
      EatTime = new DateTimeOffset(eatAt).ToUnixTimeSeconds()
    };

    threads.Insert(config);
  }

  public static void DeleteIfExists(ulong channelID) {
    threads.DeleteMany(x => x.ChannelID == channelID);
  }

  public static async Task<(IGuildChannel, DateTime)?> GetThread(ulong channelID) {
    ThreadConfig? config = threads.Find(x => x.ChannelID == channelID).FirstOrDefault();
    return config == null ? null : await GetThread(config);
  }

  private static async Task<(IGuildChannel, DateTime)?> GetThread(ThreadConfig config) {
    IGuildChannel? channel = await DiscordHelper.GetChannel(config.GuildID, config.ChannelID);
    if (channel == null) {
      DeleteIfExists(config.ChannelID);
      return null;
    }

    DateTime eatAt = DateTimeOffset.FromUnixTimeSeconds(config.EatTime).UtcDateTime;
    return (channel, eatAt);
  }

  public static async Task<List<(IGuildChannel, DateTime)>> GetThreads() {
    List<(IGuildChannel, DateTime)> allThreads = new();

    foreach (ThreadConfig config in threads.FindAll()) {
      (IGuildChannel, DateTime)? thread = await GetThread(config);
      if (thread != null) {
        allThreads.Add(((IGuildChannel, DateTime))thread);
      }
    }

    return allThreads;
  }
}
