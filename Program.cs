﻿using Discord;
using Discord.WebSocket;
using Fidobot.Controllers;
using Fidobot.Services;
using Fidobot.Utilities;
using Microsoft.Extensions.Configuration;
using Timer = System.Timers.Timer;

namespace Fidobot;

public class Program {
  public static async Task Main() {
    DiscordHelper.client.Ready += Client_Ready;
    DiscordHelper.client.SlashCommandExecuted += Client_SlashCommandExecuted;
    DiscordHelper.client.Log += Log;

    IConfigurationRoot? config = new ConfigurationBuilder()
      .AddUserSecrets<Program>(optional: true)
      .AddEnvironmentVariables("FIDO_TOKEN")
      .Build();

    await DiscordHelper.client.LoginAsync(TokenType.Bot, config.GetSection("FIDO_TOKEN").Value);
    await DiscordHelper.client.StartAsync();

    await Task.Delay(-1);
  }

  private static async Task Client_Ready() {
    await CommandsController.CreateCommands();

    Timer eatChecker = new(3 * 1000); // TODO: Make this value configurable
    eatChecker.Elapsed += EatCheckerElapsed;
    eatChecker.Enabled = true;
  }

  private static Task Client_SlashCommandExecuted(SocketSlashCommand cmd) {
    switch (cmd.Data.Name) {
      case "eat":
        CommandsController.EatHandler(cmd);
        break;
      case "donteat":
        CommandsController.DontEatHandler(cmd);
        break;
      case "sniff":
        CommandsController.SniffHandler(cmd);
        break;
    }

    return Task.CompletedTask;
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
