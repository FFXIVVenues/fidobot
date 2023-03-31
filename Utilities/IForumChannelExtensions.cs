using Discord;

namespace Fidobot.Utilities;

public static class IForumChannelExtensions {
  public static async Task<List<IGuildChannel>> GetAllThreads(this IForumChannel forum) {
    List<IGuildChannel> threads = new();

    foreach (IGuildChannel thread in await forum.GetActiveThreadsAsync()) {
      threads.Add(thread);
    }

    foreach (IGuildChannel thread in await forum.GetPublicArchivedThreadsAsync()) {
      threads.Add(thread);
    }

    return threads;
  }
}
