using Discord;
using Discord.Net;
using Discord.WebSocket;
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
