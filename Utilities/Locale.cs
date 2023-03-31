namespace Fidobot.Utilities;

public class Locale {
  private static readonly Dictionary<string, string> messages = new()
  {
    // Generic
    { "yes", "Yes" },
    { "no", "No" },
    { "seconds", "Second(s)" },
    { "minutes", "Minute(s)" },
    { "hours", "Hour(s)" },
    { "days", "Day(s)" },

    // Commands
    { "eatcmd-description", "Eat this thread or configure specified forum for Fido." },
    { "eatcmd-timetype-description", "Time configuration type" },
    { "eatcmd-timevalue-description", "Time configuration value" },
    { "eatcmd-channel-description", "Specify a thread or forum." },
    { "eatcmd-eatexisting-description", "(Forum only) Should Fido eat existing threads with given time options ?" },
    { "eatcmd-eatfuture-description", "(Forum only) Should Fido eat future threads with given time options ?" },
    { "donteatcmd-description", "Disables Fido from eating this channel or forum." },
    { "donteatcmd-channel-description", "Specify a thread or forum." },
    { "sniffcmd-description", "Lists configured forums and threads." },

    // Outputs
    { "output-missingperm-server", "**ERROR:** You don't have the necessary permissions on this server to execute that command." },
    { "output-missingperm-channel", "**ERROR:** You don't have the necessary permissions on this channel to execute that command." },
    { "output-timetype-invalid", "**ERROR:** The time type you provided is invalid: {0}" },
    { "output-timevalue-invalid", "**ERROR:** Time value is invalid (0 is invalid): {0}" },
    { "output-eatexisting-notforum", "**ERROR:** Eat existing can only be enabled on Forum channels." },
    { "output-eatfuture-notforum", "**ERROR:** Eat future can only be enabled on Forum channels." },
    { "output-whatdoido", "**ERROR:** So you don't want me to eat existing channels, and you don't want me to eat future channels either...\r\n" +
      "Are you trying to starve me ?!" },

    { "output-eat-wrongchanneltype", "**ERROR:** `/eat` can only be used on Forums or Threads. I'm a picky eater." },
    { "output-eat-thread-success", "Noted ! I will eat {0} in {1}. I'm salivating already..." },
    { "output-eat-thread-overwrote", "I already planned to eat this thread in {0}.\r\n" +
      "But fine, I will eat '#{1}' in {2} instead.\r\n" +
      "Pesky humans, messing with my eating schedule..." },
    { "output-eat-thread-forumoverride", "Because this thread is inside a forum I have on my feeding schedule, I was supposed to eat it in {0}.\r\n" +
      "But fine, I will eat '#{1}' in {2} instead.\r\n" +
      "Pesky humans, messing with my eating schedule..." },
    { "output-eat-forum-success", "'#{0}' just for me ? Yaaay ! I will eat its threads {1} after their creation.\r\n" +
      "Have I eaten existing threads ? **{2}**\r\n" +
      "Will I eat future threads ? **{3}**" },
    { "output-eat-forum-overwrote", "I already had '#{0}' on my feeding schedule, I was supposed to eat its threads {1} after their creation.\r\n" +
      "But fine, I will change my schedule and eat them {2} after their creation instead.\r\n" +
      "Pesky humans, messing with my eating schedule..." },

    { "output-donteat-wrongchanneltype", "**ERROR:** `/donteat` can only be used on Forums or Threads. I'm a picky eater." },
    { "output-donteat-thread-success", "Okaaayyy... I won't eat that thread." },
    { "output-donteat-thread-notfound", "Was I supposed to eat that thread ? It wasn't on my feeding schedule..." },
    { "output-donteat-thread-backtoforum", "Okaaayyy... I will only eat that thread in {0} now because it is in a forum that is on my feeding schedule." },
    { "output-donteat-thread-notfoundbutinforum", "Was I supposed to eat that thread outside of its forum feeding schedule which is in {0} ? It wasn't on my feeding schedule..." },
    { "output-donteat-forum-success", "Okaaayyy... I won't eat the threads in this forum." },
    { "output-donteat-forum-notfound", "Was I supposed to eat the threads in this forum ? They weren't on my feeding schedule..." },

    { "output-sniff-outsideguild", "This method can only be used inside a Discord Server." },
    { "output-sniff-forums-intro", "*sniff sniff*... I found these forums on my feeding schedule:\r\n" },
    { "output-sniff-forums-entry", "- '#{0}' | Eat its threads {1} after creation date\r\n" },
    { "output-sniff-forums-entry-started", "- '#{0}' | Eat its threads {1} after creation date | Only eating threads created after {2} UTC\r\n" },
    { "output-sniff-forums-none", "*sniff sniff*... I didn't find any forum on my feeding schedule.\r\n" },
    { "output-sniff-threads-intro", "\r\n*sniff sniff*... I found these threads on my feeding schedule:\r\n" },
    { "output-sniff-threads-entry", "- '#{0}' | Eat in {1}\r\n" },
    { "output-sniff-threads-none", "\r\n*sniff sniff*... I didn't find any thread on my feeding schedule.\r\n" },
    { "output-sniff-outro", "\r\nThat's my whole feeding schedule ! I'm hungry just thinking about it now..." },
  };

  public static string Get(string key, params object[] args) {
    if (messages.TryGetValue(key, out string? message)) {
      return string.Format(message, args);
    } else {
      return $"Missing locale: {key}";
    }
  }
}
