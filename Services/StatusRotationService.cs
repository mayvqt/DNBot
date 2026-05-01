using Discord;
using Discord.WebSocket;
using DNBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DNBot.Services;

public sealed class StatusRotationService(
    DiscordSocketClient client,
    BotSettingsStore settings,
    ILogger<StatusRotationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitUntilReadyAsync(stoppingToken);

            var index = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = settings.Current.StatusMessages;
                if (messages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var message = messages[index++ % messages.Count];
                await client.SetActivityAsync(new Game(message));
                logger.LogDebug("Set status to {Status}", message);

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task WaitUntilReadyAsync(CancellationToken stoppingToken)
    {
        while (client.ConnectionState != ConnectionState.Connected && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
