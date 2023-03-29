using Fidobot.Models;
using Fidobot.Utilities;

namespace Fidobot.Services;

public class SniffService {
  public static async Task<List<FidoThread>> GetThreads(ulong guildID) {
    List<FidoThread> threads = new();
    foreach (FidoThread thread in await DBHelper.GetThreads()) {
      if (thread.Channel.GuildId !=  guildID) {
        continue;
      }

      threads.Add(thread);
    }
    return threads;
  }

  public static async Task<List<FidoForum>> GetForums(ulong guildID) {
    List<FidoForum> forums = new();
    foreach (FidoForum forum in await DBHelper.GetForums()) {
      if (forum.Channel.GuildId != guildID) {
        continue;
      }

      forums.Add(forum);
    }
    return forums;
  }
}
