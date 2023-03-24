using Discord;
using Fidobot.Models;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class ThreadService {
  public enum Result {
    Success,
    WrongChannelType,
    Overwrote
  }

  public static async void CheckThreads() {
    foreach (FidoThread thread in await DBHelper.GetThreads()) {
      if (thread.EatDT <= DateTime.UtcNow) {
        await EatThread(thread);
      }
    }
  }

  public static async Task<(Result, DateTime?)> EatThread(IGuildChannel threadChannel, DateTime eatDT) {
    if (threadChannel.GetChannelType() != ChannelType.PublicThread) {
      Console.WriteLine("[ThreadService] ERROR: Wrong channel type in EatThread: " + threadChannel.GetChannelType());
      return (Result.WrongChannelType, null);
    }

    if (eatDT <= DateTime.UtcNow) {
      return (await Eat(threadChannel), null);
    }
    return await SaveThread(threadChannel, eatDT);
  }

  private static async Task<(Result, DateTime?)> EatThread(FidoThread thread) {
    return await EatThread(thread.Channel, thread.EatDT);
  }

  private static async Task<Result> Eat(IGuildChannel thread) {
    await thread.DeleteAsync();
    DBHelper.DeleteIfExists(thread.Id);
    Console.Write("[ThreadService] Munching on #" + thread.Name + " (" + thread.Id + "), yummy !");

    return Result.Success;
  }

  private static async Task<(Result, DateTime?)> SaveThread(IGuildChannel threadChannel, DateTime eatTime) {
    FidoThread? thread = await DBHelper.GetThread(threadChannel.Id);
    Result res = Result.Success;
    DateTime? overwrote = null;

    if (thread != null) {
      DBHelper.DeleteIfExists(threadChannel.Id);
      res = Result.Overwrote;
      overwrote = thread.EatDT;
    }
    DBHelper.SaveThreadToDB(threadChannel.GuildId, threadChannel.Id, eatTime);

    return (res, overwrote);
  }
}
