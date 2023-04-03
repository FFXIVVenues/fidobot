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
    eatCmdBuilder.WithDescription(FidoStrings.eatcmd_description);
    eatCmdBuilder.AddOption(new SlashCommandOptionBuilder()
      .WithName("time-type")
      .WithDescription(FidoStrings.eatcmd_timetype_description)
      .WithRequired(true)
      .AddChoice(FidoStrings.seconds, (long)1)
      .AddChoice(FidoStrings.minutes, (long)60)
      .AddChoice(FidoStrings.hours, (long)60 * 60)
      .AddChoice(FidoStrings.days, (long)60 * 60 * 24)
      .WithType(ApplicationCommandOptionType.Integer)
    );
    eatCmdBuilder.AddOption("time-value", ApplicationCommandOptionType.Integer, FidoStrings.eatcmd_timevalue_description, true);
    eatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, FidoStrings.eatcmd_channel_description, false, null, false, null, null, null, channels);
    eatCmdBuilder.AddOption("eat-existing", ApplicationCommandOptionType.Boolean, FidoStrings.eatcmd_eatexisting_description, false);
    eatCmdBuilder.AddOption("eat-future", ApplicationCommandOptionType.Boolean, FidoStrings.eatcmd_eatfuture_description, false);

    // Build donteat command
    SlashCommandBuilder donteatCmdBuilder = new();
    donteatCmdBuilder.WithName("donteat");
    donteatCmdBuilder.WithDescription(FidoStrings.donteatcmd_description);
    donteatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, FidoStrings.donteatcmd_channel_description, false, null, false, null, null, null, channels);

    // Build sniff command
    SlashCommandBuilder sniffCmdBuilder = new();
    sniffCmdBuilder.WithName("sniff");
    sniffCmdBuilder.WithDescription(FidoStrings.sniffcmd_description);

    // Create commands
    try {
      foreach (SocketApplicationCommand cmd in await DiscordHelper.client.GetGlobalApplicationCommandsAsync()) {
        await cmd.DeleteAsync();
      }

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
        await cmd.RespondAsync(FidoStrings.output_missingperm_server, null, false, true);
      } else if (channel.GetChannelType() == ChannelType.PublicThread) {
        await cmd.RespondAsync(FidoStrings.output_missingperm_channel, null, false, true);
      } else {
        await cmd.RespondAsync(FidoStrings.output_channel_wrongtype, null, false, true);
      }
      return false;
    }
    return true;
  }

  private static bool ValidateEatArguments(long timeType, long timeValue, ref IGuildChannel? channel, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
    // If time type is zero or not equal to predefined values, respond error message
    if (timeType == 0 || (timeType != 1 && timeType != 60 && timeType != (long)60 * 60 && timeType != (long)60 * 60 * 24)) {
      cmd.RespondAsync(string.Format(FidoStrings.output_timetype_invalid, timeType), null, false, true);
      return false;
    }

    // If time value is wrong, respond with an error message
    if (timeValue <= 0 || (timeValue * timeType) > TimeSpan.MaxValue.TotalSeconds) {
      cmd.RespondAsync(string.Format(FidoStrings.output_timevalue_invalid, timeValue), null, false, true);
      return false;
    }

    // If no channel is specified, use the current channel
    channel ??= (IGuildChannel)cmd.Channel;

    // If channel is not a forum or thread, respond with an error message
    ChannelType? cType = channel.GetChannelType();
    if (cType != ChannelType.Forum && cType != ChannelType.PublicThread) {
      cmd.RespondAsync(FidoStrings.output_channel_wrongtype, null, false, true);
      return false;
    }

    // If eatExisting is true but channel isn't a forum, respond with an error message
    if (eatExisting && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync(FidoStrings.output_eatexisting_notforum, null, false, true);
      return false;
    }

    // If eatFuture is true but channel isn't a forum, respond with an error message
    if (eatFuture && channel.GetChannelType() != ChannelType.Forum) {
      cmd.RespondAsync(FidoStrings.output_eatfuture_notforum, null, false, true);
      return false;
    }

    // If both eatExisting and eatFuture are disabled on forum
    if (channel.GetChannelType() == ChannelType.Forum && !eatExisting && !eatFuture) {
      cmd.RespondAsync(FidoStrings.output_whatdoido, null, false, true);
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
        await cmd.RespondAsync(string.Format(FidoStrings.output_eat_thread_success, channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
      case ThreadService.Result.WrongChannelType:
        await cmd.RespondAsync(FidoStrings.output_eat_wrongchanneltype, null, false, true);
        break;
      case ThreadService.Result.Overwrote:
        TimeSpan overwroteETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync(string.Format(FidoStrings.output_eat_thread_overwrote, overwroteETA.ToDynamicString(), channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
      case ThreadService.Result.ForumOverride:
        TimeSpan forumOverrideETA = (DateTime)res.Item2! - DateTime.UtcNow;
        await cmd.RespondAsync(string.Format(FidoStrings.output_eat_thread_forumoverride, forumOverrideETA.ToDynamicString(), channel.Name, eatIn.ToDynamicString()), null, false, true);
        break;
    }
  }

  private static async void EatForumResponse(IGuildChannel channel, TimeSpan eatIn, bool eatExisting, bool eatFuture, (ForumService.Result, TimeSpan?) res, SocketSlashCommand cmd) {
    switch (res.Item1) {
      case ForumService.Result.Success:
        string eatExistingResult = eatExisting ? FidoStrings.yes : FidoStrings.no;
        string eatFutureResult = eatFuture ? FidoStrings.yes : FidoStrings.no;
        await cmd.RespondAsync(string.Format(FidoStrings.output_eat_forum_success, channel.Name, eatIn.ToDynamicString(), eatExistingResult, eatFutureResult), null, false, true);
        break;
      case ForumService.Result.WrongChannelType:
        await cmd.RespondAsync(FidoStrings.output_eat_wrongchanneltype, null, false, true);
        break;
      case ForumService.Result.Overwrote:
        TimeSpan overwroteETA = (TimeSpan)res.Item2!;
        await cmd.RespondAsync(string.Format(FidoStrings.output_eat_forum_overwrote, channel.Name, overwroteETA.ToDynamicString(), eatIn.ToDynamicString()), null, false, true);
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
          await cmd.RespondAsync(FidoStrings.output_donteat_thread_success, null, false, true);
          break;
        case ThreadService.Result.NotFound:
          await cmd.RespondAsync(FidoStrings.output_donteat_thread_notfound, null, false, true);
          break;
        case ThreadService.Result.BackToForum:
          string backToForumETA = ((TimeSpan)res.Item2!).ToDynamicString();
          await cmd.RespondAsync(string.Format(FidoStrings.output_donteat_thread_backtoforum, backToForumETA), null, false, true);
          break;
        case ThreadService.Result.NotFoundButInForum:
          string notFoundButInForumETA = ((TimeSpan)res.Item2!).ToDynamicString();
          await cmd.RespondAsync(string.Format(FidoStrings.output_donteat_thread_notfoundbutinforum, notFoundButInForumETA), null, false, true);
          break;
      }
    } else if (channel.GetChannelType() == ChannelType.Forum) { // If Forum
      ForumService.Result res = ForumService.DontEat(channel);
      if (res == ForumService.Result.Success) {
        await cmd.RespondAsync(FidoStrings.output_donteat_forum_success, null, false, true);
      } else {
        await cmd.RespondAsync(FidoStrings.output_donteat_forum_notfound, null, false, true);
      }
    } else { // If not thread or forum
      await cmd.RespondAsync(FidoStrings.output_donteat_wrongchanneltype, null, false, true);
    }
  }

  public static async void SniffHandler(SocketSlashCommand cmd) {
    if (cmd.GuildId == null) {
      await cmd.RespondAsync("This method can only be used in a Discord Server.", null, false, true);
      return;
    }

    if (!DiscordHelper.CanManageThreads((SocketGuildUser)cmd.User)) {
      await cmd.RespondAsync(FidoStrings.output_missingperm_server, null, false, true);
      return;
    }

    string final = "";
    List<FidoForum> forums = await SniffService.GetForums((ulong)cmd.GuildId);
    if (forums.Count > 0) {
      final += FidoStrings.output_sniff_forums_intro;
      foreach (FidoForum forum in forums) {
        if (!forum.EatExisting) {
          final += string.Format(FidoStrings.output_sniff_forums_entry_started, forum.Channel.Name, forum.EatOffset.ToDynamicString(), forum.Started);
        } else {
          final += string.Format(FidoStrings.output_sniff_forums_entry, forum.Channel.Name, forum.EatOffset.ToDynamicString(), forum.EatExisting, forum.Started);
        }
      }
    } else {
      final += FidoStrings.output_sniff_forums_none;
    }

    List<FidoThread> threads = await SniffService.GetThreads((ulong)cmd.GuildId);
    if (threads.Count > 0) {
      final += FidoStrings.output_sniff_threads_intro;
      foreach (FidoThread thread in threads) {
        TimeSpan eatIn = thread.EatDT - DateTime.UtcNow;
        final += string.Format(FidoStrings.output_sniff_threads_entry, thread.Channel.Name, eatIn.ToDynamicString());
      }
    } else {
      final += FidoStrings.output_sniff_threads_none;
    }

    final += FidoStrings.output_sniff_outro;
    await cmd.RespondAsync(final, null, false, true);
  }
}
