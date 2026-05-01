using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DNBot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DNBot.Services;

public sealed class DiscordBotService(
    DiscordSocketClient client,
    InteractionService interactions,
    CommandService commands,
    IServiceProvider services,
    BotSettingsStore settings,
    ILogger<DiscordBotService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Current.Token))
        {
            logger.LogWarning("Discord token is missing. Open the dashboard, save a token, then restart the app.");
            return;
        }

        client.Log += LogDiscordAsync;
        commands.Log += LogDiscordAsync;
        interactions.Log += LogDiscordAsync;

        client.Ready += RegisterInteractionsAsync;
        client.MessageReceived += HandleMessageAsync;
        client.InteractionCreated += HandleInteractionAsync;

        await commands.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
        await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);

        logger.LogInformation("Starting Discord client");
        await client.LoginAsync(TokenType.Bot, settings.Current.Token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Discord client");

        client.Ready -= RegisterInteractionsAsync;
        client.MessageReceived -= HandleMessageAsync;
        client.InteractionCreated -= HandleInteractionAsync;

        await client.StopAsync();
        await client.LogoutAsync();
    }

    private async Task RegisterInteractionsAsync()
    {
        if (settings.Current.DevelopmentGuildId is { } guildId)
        {
            await interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
            logger.LogInformation("Registered slash commands to development guild {GuildId}", guildId);
            return;
        }

        await interactions.RegisterCommandsGloballyAsync(deleteMissing: true);
        logger.LogInformation("Registered slash commands globally; Discord may take up to an hour to show changes");
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        using var scope = services.CreateScope();
        var context = new SocketInteractionContext(client, interaction);
        var result = await interactions.ExecuteCommandAsync(context, scope.ServiceProvider);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Interaction {InteractionId} failed: {Error} {Reason}",
                interaction.Id,
                result.Error,
                result.ErrorReason);
        }
    }

    private async Task HandleMessageAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message || message.Author.IsBot)
        {
            return;
        }

        var position = 0;
        var mentioned = message.HasMentionPrefix(client.CurrentUser, ref position);
        var prefixed = message.HasStringPrefix(settings.Current.Prefix, ref position);

        if (!mentioned && !prefixed)
        {
            return;
        }

        using var scope = services.CreateScope();
        var context = new SocketCommandContext(client, message);
        var result = await commands.ExecuteAsync(context, position, scope.ServiceProvider);

        if (!result.IsSuccess && result.Error is not CommandError.UnknownCommand)
        {
            logger.LogWarning("Prefix command failed: {Error} {Reason}", result.Error, result.ErrorReason);
            await message.ReplyAsync($"Command failed: {result.ErrorReason}");
        }
    }

    private Task LogDiscordAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
