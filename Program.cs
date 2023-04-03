using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;
using Fidobot.Controllers;
using Fidobot.Services;
using Fidobot.Utilities;
using Microsoft.Extensions.Configuration;
using Timer = System.Timers.Timer;

namespace Fidobot;

public class Program {
  public static IConfigurationRoot Configuration { get; }

  static Program() {
    Configuration = new ConfigurationBuilder()
      .AddUserSecrets<Program>(optional: true)
      .AddEnvironmentVariables("FIDO_")
      .Build();
  }

  public static async Task Main() {
    DiscordHelper.client.Ready += Client_Ready;
    DiscordHelper.client.SlashCommandExecuted += Client_SlashCommandExecuted;
    DiscordHelper.client.Log += Log;

    await DiscordHelper.client.LoginAsync(TokenType.Bot, Configuration.GetValue<string>("TOKEN"));
    await DiscordHelper.client.StartAsync();

    await Task.Delay(-1);
  }

  private static async Task Client_Ready() {
    // await CommandsController.CreateCommands();

    Timer eatChecker = new(3 * 1000); // TODO: Make this value configurable
    eatChecker.Elapsed += EatCheckerElapsed;
    eatChecker.Enabled = true;
  }

  private static async Task Client_SlashCommandExecuted(SocketSlashCommand cmd)
  {
    await cmd.DeferAsync(ephemeral: true);
    switch (cmd.Data.Name)
    {
      case "eat":
        await CommandsController.EatHandler(cmd);
        break;
      case "donteat":
        await CommandsController.DontEatHandler(cmd);
        break;
      case "sniff":
        await CommandsController.SniffHandler(cmd);
        break;
    }
  }

  public static Task Log(LogMessage msg) {
    Console.WriteLine(msg.ToString());
    return Task.CompletedTask;
  }

  private static void EatCheckerElapsed(object? sender, System.Timers.ElapsedEventArgs e) {
    ThreadService.CheckThreads();
    ForumService.CheckForums();
  }
}
