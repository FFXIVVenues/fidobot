using Discord;
using Fidobot.Models;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class ThreadService {
  public enum Result {
    Success,
    WrongChannelType,
    Overwrote,
    ForumOverride,
    NotFound
  }

  public static async void CheckThreads() {
    foreach (FidoThread thread in await DBHelper.GetThreads()) {
      if (thread.EatDT <= DateTime.UtcNow) {
        await EatThread(thread);
      }
    }
  }

  public static async Task<(Result, DateTime?)> EatThread(IGuildChannel threadChannel, DateTime eatDT, bool store = true) {
    if (threadChannel.GetChannelType() != ChannelType.PublicThread) {
      Console.Error.WriteLine("[ThreadService] ERROR: Wrong channel type in EatThread: " + threadChannel.GetChannelType());
      return (Result.WrongChannelType, null);
    }

    if (eatDT <= DateTime.UtcNow) {
      return (await Eat(threadChannel), null);
    }
    if (store) {
      return await SaveThread(threadChannel, eatDT);
    }
    return (Result.Success, null);
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

    FidoForum? forum = await DBHelper.GetForum(((IThreadChannel)threadChannel).GetParentForum().Id);
    if (forum != null) {
      res = Result.ForumOverride;
      overwrote = (threadChannel.CreatedAt + forum.EatOffset).UtcDateTime;
    }

    DBHelper.SaveThreadToDB(threadChannel.GuildId, threadChannel.Id, eatTime);
    return (res, overwrote);
  }

  public static Result DontEat(IGuildChannel channel) {
    if (!DBHelper.ThreadExists(channel.Id)) {
      return Result.NotFound;
    }
    DBHelper.DeleteIfExists(channel.Id);
    return Result.Success;
  }
}
