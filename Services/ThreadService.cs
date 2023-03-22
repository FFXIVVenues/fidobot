using Discord;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class ThreadService {
  public static async void EatThread(IGuildChannel thread, DateTime eatTime) {
    if (thread.GetChannelType() != ChannelType.PublicThread) {
      Console.WriteLine("[ThreadService] ERROR: Wrong channel type in EatThread: " + thread.GetChannelType());
      return;
    }

    if (eatTime <= DateTime.UtcNow) {
      await thread.DeleteAsync();
      DBHelper.RemoveIfExists(thread.Id);
      Console.Write("[ThreadService] Munching on #" + thread.Name + ", yummy !");
    } else {
      DBHelper.SaveThreadToDB(thread.GuildId, thread.Id, eatTime);
    }
  }

  public static async void CheckThreads() {
    foreach ((IGuildChannel, DateTime) thread in await DBHelper.GetThreads()) {
      if (thread.Item2 <= DateTime.UtcNow) {
        EatThread(thread.Item1, thread.Item2);
      }
    }
  }
}
