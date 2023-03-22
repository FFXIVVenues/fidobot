using Discord;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class ThreadService {
  public enum Result {
    Success,
    WrongChannelType,
    Overwrote
  }

  public static async void CheckThreads() {
    foreach ((IGuildChannel, DateTime) thread in await DBHelper.GetThreads()) {
      if (thread.Item2 <= DateTime.UtcNow) {
        await EatThread(thread.Item1, thread.Item2);
      }
    }
  }

  public static async Task<(Result, DateTime?)> EatThread(IGuildChannel thread, DateTime eatTime) {
    if (thread.GetChannelType() != ChannelType.PublicThread) {
      Console.WriteLine("[ThreadService] ERROR: Wrong channel type in EatThread: " + thread.GetChannelType());
      return (Result.WrongChannelType, null);
    }

    if (eatTime <= DateTime.UtcNow) {
      return (await Eat(thread), null);
    }
    return await SaveThread(thread, eatTime);
  }

  private static async Task<Result> Eat(IGuildChannel thread) {
    await thread.DeleteAsync();
    DBHelper.DeleteIfExists(thread.Id);
    Console.Write("[ThreadService] Munching on #" + thread.Name + " (" + thread.Id + "), yummy !");

    return Result.Success;
  }

  private static async Task<(Result, DateTime?)> SaveThread(IGuildChannel thread, DateTime eatTime) {
    (IGuildChannel, DateTime)? dbThread = await DBHelper.GetThread(thread.Id);
    Result res = Result.Success;
    DateTime? overwrote = null;

    if (dbThread != null) {
      DBHelper.DeleteIfExists(thread.Id);
      res = Result.Overwrote;
      overwrote = dbThread.Value.Item2;
    }
    DBHelper.SaveThreadToDB(thread.GuildId, thread.Id, eatTime);

    return (res, overwrote);
  }
}
