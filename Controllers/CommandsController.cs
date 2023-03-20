using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Fidobot.Controllers;

public class CommandsController {
  public static async Task CreateCommands(DiscordSocketClient client) { // Content is currently testing, ignore
    // Build eat command
    SlashCommandBuilder eatCmdBuilder = new();
    eatCmdBuilder.WithName("eat");
    eatCmdBuilder.WithDescription("Eat this channel in X hours.");
    List<ChannelType> channels = new() { ChannelType.PublicThread, ChannelType.Forum };
    eatCmdBuilder.AddOption("fidotest", ApplicationCommandOptionType.Channel, "Testing", true, null, false, null, null, null, channels);
    eatCmdBuilder.AddOption("time", ApplicationCommandOptionType.Integer, "In how many hours ?", true);
    eatCmdBuilder.AddOption("channel", ApplicationCommandOptionType.Channel, "Which channel ? (Default: current)", false);

    // Build donteat command
    SlashCommandBuilder donteatCmdbuilder = new();
    donteatCmdbuilder.WithName("donteat");
    donteatCmdbuilder.WithDescription("Disables fidobot from eating this channel.");

    // Create commands
    try {
      await client.CreateGlobalApplicationCommandAsync(eatCmdBuilder.Build());
      await client.CreateGlobalApplicationCommandAsync(donteatCmdbuilder.Build());
    } catch (HttpException ex) {
      string json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
      Console.WriteLine(json);
    }

    await Program.Log(new LogMessage(LogSeverity.Verbose, "CommandsController", "Commands created successfully."));
  }

  public static void EatHandler(SocketSlashCommand cmd) { // Content is currently testing, ignore
    long hours = 0;
    IGuildChannel? channel = null;

    // Loop through command options and assign values to variables
    foreach (SocketSlashCommandDataOption option in cmd.Data.Options) {
      switch (option.Name) {
        case "time":
          hours = (long)option.Value;
          break;
        case "channel":
          channel = (IGuildChannel)option.Value;
          break;
      }
    }

    Console.WriteLine("eat command used in : " + cmd.Channel.GetChannelType());

    // If hours is less than or equal to 0, respond with an error message
    if (hours <= 0) {
      cmd.RespondAsync("Hours cannot be 0.", null, false, true);
      return;
    }

    // If no channel is specified, use the current channel
    channel ??= (IGuildChannel)cmd.Channel;

    cmd.RespondAsync("Will eat #" + channel.Name + " in " + hours + " hours.", null, false, true);
  }

  public static void DontEatHandler(SocketSlashCommand cmd) { // Content is currently testing, ignore
    cmd.RespondAsync("Will not eat this channel.");
  }
}
