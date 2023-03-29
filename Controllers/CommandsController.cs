using Discord;
using Discord.Net;
using Discord.WebSocket;
using Fidobot.Models;
using Fidobot.Services;
using Fidobot.Utilities;
using Newtonsoft.Json;

namespace Fidobot.Controllers;

public class CommandsController {
  public static async Task CreateCommands() {
    List<ChannelType> channels = new() { ChannelType.PublicThread, ChannelType.Forum };

    // Build eat command
    SlashCommandBuilder eatCmdBuilder = new();
    eatCmdBuilder.WithName("eat");
    eatCmdBuilder.WithDescription("Eat this thread or configure specified forum for Fido.");
    eatCmdBuilder.AddOption(new SlashCommandOptionBuilder()
       .WithName("time-type")
       .WithDescription("Time configuration type")
       .WithRequired(true)
       .AddChoice("Seconds", (long)1)
       .AddChoice("Minutes", (long)60)
       .AddChoice("Hours", (long)60 * 60)
       .AddChoice("Days", (long)60 * 60 * 24)
       .WithType(ApplicationCommandOptionType.Integer)
    );
    eatCmdBuilder.AddOption("time-value", ApplicationCommandOptionType.Integer, "Time configuration value", true);
    eatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, "Specify a thread or forum.", false, null, false, null, null, null, channels);
    eatCmdBuilder.AddOption("eat-existing", ApplicationCommandOptionType.Boolean, "(Forum only) Should Fido eat existing threads with given time options ?", false);
    eatCmdBuilder.AddOption("eat-future", ApplicationCommandOptionType.Boolean, "(Forum only) Should Fido eat future threads with given time options ?", false);

    // Build donteat command
    SlashCommandBuilder donteatCmdBuilder = new();
    donteatCmdBuilder.WithName("donteat");
    donteatCmdBuilder.WithDescription("Disables fidobot from eating this channel.");
    donteatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, "Specify a thread or forum.", false, null, false, null, null, null, channels);

    // Build sniff command
    SlashCommandBuilder sniffCmdBuilder = new();
    sniffCmdBuilder.WithName("sniff");
    sniffCmdBuilder.WithDescription("List configured forums and threads.");

    // Create commands
    try {
      await DiscordHelper.client.CreateGlobalApplicationCommandAsync(eatCmdBuilder.Build());
      await DiscordHelper.client.CreateGlobalApplicationCommandAsync(donteatCmdBuilder.Build());
      await DiscordHelper.client.CreateGlobalApplicationCommandAsync(sniffCmdBuilder.Build());
    } catch (HttpException ex) {
      string json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
      Console.WriteLine(json);
    }

    Console.WriteLine("[CommandsController] Commands created successfully.");
  }

  public static void EatHandler(SocketSlashCommand cmd) {
    long timeType = 0;
    long timeValue = 0;
    IGuildChannel? channel = null;
    bool eatExisting = false;
    bool eatFuture = false;

    // Loop through command options and assign values to variables
    foreach (SocketSlashCommandDataOption option in cmd.Data.Options) {
      switch (option.Name) {
        case "time-type":
          timeType = (long)option.Value;
          break;
        case "time-value":
          timeValue = (long)option.Value;
          break;
        case "channel":
          channel = (IGuildChannel)option.Value;
          break;
        case "eat-existing":
          eatExisting = (bool)option.Value;
          break;
        case "eat-future":
          eatFuture = (bool)option.Value;
          break;
      }
    }

    if (ValidateEatArguments(timeType, timeValue, ref channel, eatExisting, eatFuture, cmd)) {
      Eat(channel!, timeType, timeValue, eatExisting, eatFuture, cmd);
    }
  }

  private static bool ValidateEatArguments(long timeType, long timeValue, ref IGuildChannel? channel, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
    // If time type is zero or not equal to predefined values, log and respond error message
    if (timeType == 0 || (timeType != 1 && timeType != 60 && timeType != (long)60 * 60 && timeType != (long)60 * 60 * 24)) {
      cmd.RespondAsync("An error occurred. timeType value wrong: " + timeType, null, false, true);
      Console.WriteLine("[CommandsController] ERROR: timeType value wrong: " + timeType);
      return false;
    }

    // If time value is wrong, log and respond with an error message
    if (timeValue <= 0) {
      cmd.RespondAsync("Hours cannot be 0.", null, false, true);
      Console.WriteLine("[CommandsController] ERROR: timeValue value wrong: " + timeValue);
      return false;
    }

    // If no channel is specified, use the current channel
    channel ??= (IGuildChannel)cmd.Channel;

    // If eatExisting is true but channel isn't a forum, respond with an error message
    if (eatExisting && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync("Eat existing can only be enabled on forum channels.");
      return false;
    }

    // If eatFuture is true but channel isn't a forum, respond with an error message
    if (eatFuture && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync("Eat Future can only be enabled on forum channels.");
      return false;
    }

    return true;
  }

  private static async void Eat(IGuildChannel channel, long timeType, long timeValue, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
    TimeSpan eatIn = TimeSpan.FromSeconds(timeValue * timeType);
    DateTime eatTime = DateTime.UtcNow + eatIn;

    if (channel.GetChannelType() == ChannelType.PublicThread) { // If thread
      (ThreadService.Result, DateTime?) res = await ThreadService.EatThread(channel, eatTime);
      EatThreadResponse(channel, eatIn, res, cmd);
    } else { // If Forum
      (ForumService.Result, TimeSpan?) res = await ForumService.EatForum(channel, eatIn, eatExisting, eatFuture);
      EatForumResponse(channel, eatIn, eatExisting, eatFuture, res, cmd);
    }
  }

  private static async void EatThreadResponse(IGuildChannel channel, TimeSpan eatIn, (ThreadService.Result, DateTime?) res, SocketSlashCommand cmd) {
    switch (res.Item1) {
      case ThreadService.Result.Success:
        await cmd.RespondAsync("Will eat #" + channel.Name + " in " + eatIn.ToDynamicString() + ".", null, false, true);
        break;
      case ThreadService.Result.WrongChannelType:
        await cmd.RespondAsync("ERROR: `/eat` can only be used on Forums or Threads.", null, false, true);
        break;
      case ThreadService.Result.Overwrote:
        TimeSpan overwroteETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync("This thread was already scheduled to be eaten in " + overwroteETA.ToDynamicString() + ".\r\n" +
          "Will eat #" + channel.Name + " in " + eatIn.ToDynamicString() + " instead.", null, false, true);
        break;
      case ThreadService.Result.ForumOverride:
        TimeSpan forumOverrideETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync("This thread is in a forum managed by Fido.\r\n" +
          "The forum configuration made it so that this thread was scheduled to be eatin in " + forumOverrideETA.ToDynamicString() + ".\r\n" +
          "Will eat #" + channel.Name + " in " + eatIn.ToDynamicString() + " instead.", null, false, true);
        break;
    }
  }

  private static async void EatForumResponse(IGuildChannel channel, TimeSpan eatIn, bool eatExisting, bool eatFuture, (ForumService.Result, TimeSpan?) res, SocketSlashCommand cmd) {
    switch (res.Item1) {
      case ForumService.Result.Success:
        await cmd.RespondAsync($"#{channel.Name} configured to eat threads {eatIn.ToDynamicString()} after their creation.\r\n" +
          $"Eating existing threads with the same rules : ${(eatExisting ? "Yes" : "No")}" +
          $"Eating future threads with the same rules : ${(eatFuture ? "Yes" : "No")}", null, false, true);
        break;
      case ForumService.Result.WrongChannelType:
        await cmd.RespondAsync("ERROR: `/eat` can only be used on Forums or Threads.", null, false, true);
        break;
      case ForumService.Result.Overwrote:
        TimeSpan overwroteETA = (TimeSpan)res.Item2!;
        await cmd.RespondAsync($"This forum was already configured to eat threads {overwroteETA.ToDynamicString()} after their creation.\r\n" +
          $"Channels will now be eaten {eatIn.ToDynamicString()} after their creation date instead.", null, false, true);
        break;
    }
  }

  public static async void SniffHandler(SocketSlashCommand cmd) {
    if (cmd.GuildId == null) {
      await cmd.RespondAsync("This method can only be used in a Discord Server.");
      return;
    }

    string final = "*sniff sniff*... I found these forums :\r\n";
    foreach (FidoForum forum in await SniffService.GetForums((ulong)cmd.GuildId)) {
      final += $"- '#{forum.Channel.Name}' | Eat threads {forum.EatOffset.ToDynamicString()} after creation date | Eat Existing; {forum.EatExisting} | Started; {forum.Started}\r\n";
    }

    final += "\r\n*sniff sniff*... Oh and these threads too :\r\n";
    foreach (FidoThread thread in await SniffService.GetThreads((ulong)cmd.GuildId)) {
      TimeSpan eatIn = thread.EatDT - DateTime.UtcNow;
      final += $"- '#{thread.Channel.Name}' | Eat in {eatIn.ToDynamicString()}\r\n";
    }
    final += "\r\nMy stomach is already gurgling...";

    await cmd.RespondAsync(final, null, false, true);
  }
}
