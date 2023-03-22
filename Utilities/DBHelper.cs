using Discord;
using Fidobot.Models;
using LiteDB;

namespace Fidobot.Utilities;

public static class DBHelper {
  private static readonly LiteDatabase db = new(@"./main.db");
  private static readonly ILiteCollection<ThreadConfig> threads = db.GetCollection<ThreadConfig>();
  private static readonly ILiteCollection<ForumConfig> forums = db.GetCollection<ForumConfig>();

  public static void SaveThreadToDB(ulong guildID, ulong threadID, DateTime eatAt) {
    ThreadConfig threadConfig = new() {
      GuildID = guildID,
      ThreadID = threadID,
      EatAt = new DateTimeOffset(eatAt).ToUnixTimeSeconds()
    };

    threads.Insert(threadConfig);
  }

  public static void RemoveIfExists(ulong threadID) {
    threads.Delete((BsonValue)threadID);
  }

  public static async Task<List<(IGuildChannel, DateTime)>> GetThreads() {
    List<(IGuildChannel, DateTime)> allThreads = new();
    foreach (ThreadConfig threadConfig in threads.FindAll()) {
      IGuild guild = Program.client.GetGuild(threadConfig.GuildID);
      IGuildChannel channel = await guild.GetChannelAsync(threadConfig.ThreadID);
      DateTime eatAt = DateTimeOffset.FromUnixTimeSeconds(threadConfig.EatAt).UtcDateTime;
      allThreads.Add((channel, eatAt));
    }
    return allThreads;
  }

  // Method : SaveForumToDB
  // etc...
}
