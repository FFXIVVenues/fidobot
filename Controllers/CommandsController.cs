using Discord;
using Discord.Net;
using Discord.WebSocket;
using Fidobot.Services;
using Fidobot.Utilities;
using Newtonsoft.Json;

namespace Fidobot.Controllers;

public class CommandsController {
  public static async Task CreateCommands(DiscordSocketClient client) {
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
    SlashCommandBuilder donteatCmdbuilder = new();
    donteatCmdbuilder.WithName("donteat");
    donteatCmdbuilder.WithDescription("Disables fidobot from eating this channel.");
    donteatCmdbuilder.AddOption("channel", ApplicationCommandOptionType.Channel, "Specify a thread or forum.", false, null, false, null, null, null, channels);

    // Create commands
    try {
      await client.CreateGlobalApplicationCommandAsync(eatCmdBuilder.Build());
      await client.CreateGlobalApplicationCommandAsync(donteatCmdbuilder.Build());
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

    if (ValidateEatArguments(timeType, timeValue, channel, eatExisting, eatFuture, cmd)) {
      Eat(channel!, timeType, timeValue, eatExisting, eatFuture, cmd);
    }
  }

  private static bool ValidateEatArguments(long timeType, long timeValue, IGuildChannel? channel, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
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

  private static void Eat(IGuildChannel channel, long timeType, long timeValue, bool eatExisting, bool eatFuture, SocketSlashCommand cmd) {
    TimeSpan eatIn = TimeSpan.FromSeconds(timeValue * timeType);
    DateTime eatTime = DateTime.UtcNow + eatIn;

    if (channel.GetChannelType() == ChannelType.PublicThread) { // If thread
      ThreadService.EatThread(channel, eatTime);
      cmd.RespondAsync("Will eat #" + channel.Name + " in " + eatIn.ToDynamicString() + ".", null, false, true);
    } else { // If Forum
      //TBD
    }
  }

  public static void DontEatHandler(SocketSlashCommand cmd) { // Content is currently testing, ignore
    cmd.RespondAsync("Will not eat this channel.");
  }
}
