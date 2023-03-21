using Discord;

namespace Fidobot.Services;

public class ThreadService {
  public static async void EatThread(IGuildChannel thread, DateTime eatTime) {
    if (thread.GetChannelType() != ChannelType.PublicThread) {
      Console.WriteLine("[ThreadService] ERROR: Wrong channel type in EatThread: " + thread.GetChannelType());
      return;
    }

    if (eatTime <= DateTime.UtcNow) {
      await thread.DeleteAsync();

      // Remove db entry if it exists

      Console.Write("[ThreadService] Munching on #" + thread.Name + ", yummy !");
    } else {
      // Save in db to eat later
    }
  }

  // Method : SaveThread
  // etc...
}
