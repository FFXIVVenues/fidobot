using Fidobot.Models;
using Fidobot.Models.DB;
using LiteDB;

namespace Fidobot.Utilities;

public static class DBHelper {
  private static readonly LiteDatabase db = new(@"./main.db");
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
      }
    }

    return allThreads;
  }
}
