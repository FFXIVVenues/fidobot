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
    eatCmdBuilder.WithDescription(Locale.Get("eatcmd-description"));
    eatCmdBuilder.AddOption(new SlashCommandOptionBuilder()
      .WithName("time-type")
      .WithDescription(Locale.Get("eatcmd-timetype-description"))
      .WithRequired(true)
      .AddChoice(Locale.Get("seconds"), (long)1)
      .AddChoice(Locale.Get("minutes"), (long)60)
      .AddChoice(Locale.Get("hours"), (long)60 * 60)
      .AddChoice(Locale.Get("days"), (long)60 * 60 * 24)
      .WithType(ApplicationCommandOptionType.Integer)
    );
    eatCmdBuilder.AddOption("time-value", ApplicationCommandOptionType.Integer, Locale.Get("eatcmd-timevalue-description"), true);
    eatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, Locale.Get("eatcmd-channel-description"), false, null, false, null, null, null, channels);
    eatCmdBuilder.AddOption("eat-existing", ApplicationCommandOptionType.Boolean, Locale.Get("eatcmd-eatexisting-description"), false);
    eatCmdBuilder.AddOption("eat-future", ApplicationCommandOptionType.Boolean, Locale.Get("eatcmd-eatfuture-description"), false);

    // Build donteat command
    SlashCommandBuilder donteatCmdBuilder = new();
    donteatCmdBuilder.WithName("donteat");
    donteatCmdBuilder.WithDescription(Locale.Get("donteatcmd-description"));
    donteatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, Locale.Get("donteatcmd-channel-description"), false, null, false, null, null, null, channels);

    // Build sniff command
    SlashCommandBuilder sniffCmdBuilder = new();
    sniffCmdBuilder.WithName("sniff");
    sniffCmdBuilder.WithDescription(Locale.Get("sniffcmd-description"));

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

  public static async void EatHandler(SocketSlashCommand cmd) {
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

    if (ValidateEatArguments(timeType, timeValue, ref channel, eatExisting, eatFuture, cmd) && await ValidatePermissions(cmd, channel!)) {
      Eat(channel!, timeType, timeValue, eatExisting, eatFuture, cmd);
    }
  }

  private static async Task<bool> ValidatePermissions(SocketSlashCommand cmd, IGuildChannel channel) {
    if (!DiscordHelper.CanManageThreads((SocketGuildUser)cmd.User, channel!)) {
      if (channel.GetChannelType() == ChannelType.Forum) {
        await cmd.RespondAsync(Locale.Get("output-missingperm-server"), null, false, true);
      } else if (channel.GetChannelType() == ChannelType.PublicThread) {
        await cmd.RespondAsync(Locale.Get("output-missingperm-channel"), null, false, true);
      }
      return false;
    }
    return true;
  }

  private static bool ValidateEatArguments(long timeType, long timeValue, ref IGuildChannel? channel, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
    // If time type is zero or not equal to predefined values, respond error message
    if (timeType == 0 || (timeType != 1 && timeType != 60 && timeType != (long)60 * 60 && timeType != (long)60 * 60 * 24)) {
      cmd.RespondAsync(Locale.Get("output-timetype-invalid", timeType), null, false, true);
      return false;
    }

    // If time value is wrong, respond with an error message
    if (timeValue <= 0 || (timeValue * timeType) > TimeSpan.MaxValue.TotalSeconds) {
      cmd.RespondAsync(Locale.Get("output-timevalue-invalid", timeValue), null, false, true);
      return false;
    }

    // If no channel is specified, use the current channel
    channel ??= (IGuildChannel)cmd.Channel;

    // If eatExisting is true but channel isn't a forum, respond with an error message
    if (eatExisting && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync(Locale.Get("output-eatexisting-notforum"), null, false, true);
      return false;
    }

    // If eatFuture is true but channel isn't a forum, respond with an error message
    if (eatFuture && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync(Locale.Get("output-eatfuture-notforum"), null, false, true);
      return false;
    }

    // If both eatExisting and eatFuture are disabled on forum
    if (channel.GetChannelType() == ChannelType.Forum && !eatExisting && !eatFuture) {
      cmd.RespondAsync(Locale.Get("output-whatdoido"), null, false, true);
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
        await cmd.RespondAsync(Locale.Get("output-eat-thread-success", channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
      case ThreadService.Result.WrongChannelType:
        await cmd.RespondAsync(Locale.Get("output-eat-wrongchanneltype"), null, false, true);
        break;
      case ThreadService.Result.Overwrote:
        TimeSpan overwroteETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync(Locale.Get("output-eat-thread-overwrote", overwroteETA.ToDynamicString(), channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
      case ThreadService.Result.ForumOverride:
        TimeSpan forumOverrideETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync(Locale.Get("output-eat-thread-forumoverride", forumOverrideETA.ToDynamicString(), channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
    }
  }

  private static async void EatForumResponse(IGuildChannel channel, TimeSpan eatIn, bool eatExisting, bool eatFuture, (ForumService.Result, TimeSpan?) res, SocketSlashCommand cmd) {
    switch (res.Item1) {
      case ForumService.Result.Success:
        string eatExistingResult = eatExisting ? Locale.Get("yes") : Locale.Get("no");
        string eatFutureResult = eatFuture ? Locale.Get("yes") : Locale.Get("no");
        await cmd.RespondAsync(Locale.Get("output-eat-forum-success", channel.Name, eatIn.ToDynamicString(), eatExistingResult, eatFutureResult), null, false, true);
        break;
      case ForumService.Result.WrongChannelType:
        await cmd.RespondAsync(Locale.Get("output-eat-wrongchanneltype"), null, false, true);
        break;
      case ForumService.Result.Overwrote:
        TimeSpan overwroteETA = (TimeSpan)res.Item2!;
        await cmd.RespondAsync(Locale.Get("output-eat-forum-overwrote", channel.Name, overwroteETA.ToDynamicString(), eatIn.ToDynamicString()), null, false, true);
        break;
    }
  }

  public static async void DontEatHandler(SocketSlashCommand cmd) {
    IGuildChannel? channel = cmd.Data.Options.FirstOrDefault()?.Value as IGuildChannel;
    channel ??= (IGuildChannel)cmd.Channel;

    if (!await ValidatePermissions(cmd, channel)) {
      return;
    }

    if (channel.GetChannelType() == ChannelType.PublicThread) { // If Thread
      (ThreadService.Result, TimeSpan?) res = await ThreadService.DontEat(channel);
      switch (res.Item1) {
        case ThreadService.Result.Success:
          await cmd.RespondAsync(Locale.Get("output-donteat-thread-success"), null, false, true);
          break;
        case ThreadService.Result.NotFound:
          await cmd.RespondAsync(Locale.Get("output-donteat-thread-notfound"), null, false, true);
          break;
        case ThreadService.Result.BackToForum:
          string backToForumETA = ((TimeSpan)res.Item2!).ToDynamicString();
          await cmd.RespondAsync(Locale.Get("output-donteat-thread-backtoforum", backToForumETA), null, false, true);
          break;
        case ThreadService.Result.NotFoundButInForum:
          string notFoundButInForumETA = ((TimeSpan)res.Item2!).ToDynamicString();
          await cmd.RespondAsync(Locale.Get("output-donteat-thread-notfoundbutinforum", notFoundButInForumETA), null, false, true);
          break;
      }
    } else if (channel.GetChannelType() == ChannelType.Forum) { // If Forum
      ForumService.Result res = ForumService.DontEat(channel);
      if (res == ForumService.Result.Success) {
        await cmd.RespondAsync(Locale.Get("output-donteat-forum-success"), null, false, true);
      } else {
        await cmd.RespondAsync(Locale.Get("output-donteat-forum-notfound"), null, false, true);
      }
    } else { // If not thread or forum
      await cmd.RespondAsync(Locale.Get("output-donteat-wrongchanneltype"), null, false, true);
    }
  }

  public static async void SniffHandler(SocketSlashCommand cmd) {
    if (cmd.GuildId == null) {
      await cmd.RespondAsync("This method can only be used in a Discord Server.", null, false, true);
      return;
    }

    if (!DiscordHelper.CanManageThreads((SocketGuildUser)cmd.User)) {
      await cmd.RespondAsync(Locale.Get("output-missingperm-server"), null, false, true);
      return;
    }

    string final = "";
    List<FidoForum> forums = await SniffService.GetForums((ulong)cmd.GuildId);
    if (forums.Count > 0) {
      final += Locale.Get("output-sniff-forums-intro");
      foreach (FidoForum forum in forums) {
        final += Locale.Get("output-sniff-forums-entry", forum.Channel.Name, forum.EatOffset.ToDynamicString(), forum.EatExisting, forum.Started);
      }
    } else {
      final += Locale.Get("output-sniff-forums-none");
    }

    List<FidoThread> threads = await SniffService.GetThreads((ulong)cmd.GuildId);
    if (threads.Count > 0) {
      final += Locale.Get("output-sniff-threads-intro");
      foreach (FidoThread thread in threads) {
        TimeSpan eatIn = thread.EatDT - DateTime.UtcNow;
        final += Locale.Get("output-sniff-threads-entry", thread.Channel.Name, eatIn.ToDynamicString());
      }
    } else {
      final += Locale.Get("output-sniff-threads-none");
    }

    final += Locale.Get("output-sniff-outro");
    await cmd.RespondAsync(final, null, false, true);
  }
}
