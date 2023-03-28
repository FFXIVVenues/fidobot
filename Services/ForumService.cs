using Discord;
using Fidobot.Models;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class ForumService {
  public enum Result {
    Success,
    WrongChannelType,
    Overwrote
  }

  public static async void CheckForums() {
    foreach (FidoForum forum in await DBHelper.GetForums()) {
      await CheckForumThreads(forum.Channel, forum.EatOffset, forum.EatExisting, forum.Started);
    }
  }

  private static async Task CheckForumThreads(IForumChannel forum, TimeSpan eatOffset, bool eatExisting, DateTime started) {
    foreach (IGuildChannel thread in await forum.GetActiveThreadsAsync()) {
      if (!eatExisting && thread.CreatedAt.UtcDateTime <= started) { // Don't eat thread if forum is configured to not eat thread before enable datetime
        continue;
      }
      if (DBHelper.ThreadExists(thread.Id)) { // Don't eat thread if it has been overriden by thread specific settings
        continue;
      }

      DateTime eatDT = (thread.CreatedAt + eatOffset).UtcDateTime;
      await ThreadService.EatThread(thread, eatDT, false);
    }
  }

  public static async Task<(Result, TimeSpan?)> EatForum(IGuildChannel channel, TimeSpan eatOffset, bool eatExisting, bool eatFuture) {
    if (channel is not IForumChannel forum) {
      // TODO: Write better error
      Console.Error.WriteLine($"[ForumService] ERROR: Wrong channel type in EatForum: {channel.GetChannelType()}");
      return (Result.WrongChannelType, null);
    }

    if (eatExisting) {
      await CheckForumThreads(forum, eatOffset, true, DateTime.UtcNow);
    }

    if (eatFuture) {
      return await SaveForum(forum, eatOffset, eatExisting);
    }
    return (Result.Success, null);
  }

  private static async Task<(Result, TimeSpan?)> SaveForum(IForumChannel forumChannel, TimeSpan eatOffset, bool eatExisting) {
    FidoForum? forum = await DBHelper.GetForum(forumChannel.Id);
    Result res = Result.Success;
    TimeSpan? overwrote = null;

    if (forum != null) {
      DBHelper.DeleteIfExists(forumChannel.Id);
      res = Result.Overwrote;
      overwrote = forum.EatOffset;
    }

    DBHelper.SaveForumToDB(forumChannel.GuildId, forumChannel.Id, eatExisting, eatOffset);
    return (res, overwrote);
  }
}
