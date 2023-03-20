using Discord;
using Discord.WebSocket;
using Fidobot.Controllers;
using Microsoft.Extensions.Configuration;

namespace Fidobot;

public class Program {
  private static readonly DiscordSocketClient client = new();

  public static async Task Main() {
    client.Ready += Client_Ready;
    client.SlashCommandExecuted += Client_SlashCommandExecuted;
    client.Log += Log;

    IConfigurationBuilder? builder = new ConfigurationBuilder().AddUserSecrets<Program>();
    IConfigurationRoot? config = builder.Build();
    string token = config.GetSection("token").Value;

    await client.LoginAsync(TokenType.Bot, token);
    await client.StartAsync();

    await Task.Delay(-1);
  }

  private static async Task Client_Ready() {
    await CommandsController.CreateCommands(client);
  }

  private static Task Client_SlashCommandExecuted(SocketSlashCommand cmd) {
    switch (cmd.Data.Name) {
      case "eat":
        CommandsController.EatHandler(cmd);
        break;
      case "donteat":
        CommandsController.DontEatHandler(cmd);
        break;
    }

    return Task.CompletedTask;
  }

  public static Task Log(LogMessage msg) {
    Console.WriteLine(msg.ToString());
    return Task.CompletedTask;
  }
}
