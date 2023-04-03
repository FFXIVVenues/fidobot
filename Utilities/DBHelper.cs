using Fidobot.Models;
using Fidobot.Models.DB;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace Fidobot.Utilities;

public static class DBHelper {
  private static readonly LiteDatabase db = new(Program.Configuration.GetValue<string>("DBPATH"));
  private static readonly ILiteCollection<DBThread> threads = db.GetCollection<DBThread>();
  private static readonly ILiteCollection<DBForum> forums = db.GetCollection<DBForum>();

  public static void SaveThreadToDB(ulong guildID, ulong channelID, DateTime eatAt) {
    DBThread config = new() {
      GuildID = guildID,
      ChannelID = channelID,
      EatTimestamp = new DateTimeOffset(eatAt).ToUnixTimeSeconds()
    };

    threads.Insert(config);
  }

  public static void SaveForumToDB(ulong guildID, ulong channelID, bool eatExisting, TimeSpan eatOffset) {
    DBForum config = new() {
      GuildID = guildID,
      ChannelID = channelID,
      StartedTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
      EatExisting = eatExisting,
      EatOffset = (ulong)eatOffset.TotalSeconds
    };

    forums.Insert(config);
  }

  public static bool ThreadExists(ulong channelID) {
    return threads.Exists(x => x.ChannelID == channelID);
  }

  public static bool ForumExists(ulong channelID) {
    return forums.Exists(x => x.ChannelID == channelID);
  }

  public static void DeleteIfExists(ulong channelID) {
    threads.DeleteMany(x => x.ChannelID == channelID);
    forums.DeleteMany(x => x.ChannelID == channelID);
  }

  public static async Task<FidoThread?> GetThread(ulong channelID) {
    DBThread? config = threads.Find(x => x.ChannelID == channelID).FirstOrDefault();

    if (config == null) {
      return null;
    }

    FidoThread? thread = await FidoThread.CreateAsync(config);
    return thread ?? null;
  }

  public static async Task<List<FidoThread>> GetThreads() {
    List<FidoThread> allThreads = new();

    foreach (DBThread config in threads.FindAll()) {
      FidoThread? thread = await FidoThread.CreateAsync(config);
      if (thread != null) {
        allThreads.Add(thread);
      } else {
        DeleteIfExists(config.ChannelID);
      }
    }

    return allThreads;
  }

  public static async Task<FidoForum?> GetForum(ulong forumID) {
    DBForum? config = forums.Find(x => x.ChannelID == forumID).FirstOrDefault();

    if (config == null) {
      return null;
    }

    FidoForum? forum = await FidoForum.CreateAsync(config);
    return forum ?? null;
  }

  public static async Task<List<FidoForum>> GetForums() {
    List<FidoForum> allForums = new();

    foreach (DBForum config in forums.FindAll()) {
      FidoForum? forum = await FidoForum.CreateAsync(config);
      if (forum != null) {
        allForums.Add(forum);
      } else {
        DeleteIfExists(config.ChannelID);
      }
    }

    return allForums;
  }
}
