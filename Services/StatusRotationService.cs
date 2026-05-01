using Discord;
using Discord.WebSocket;
using DNBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNBot.Services;

public sealed class StatusRotationService(
    DiscordSocketClient client,
    IOptions<DiscordBotOptions> options,
    ILogger<StatusRotationService> logger) : BackgroundService
{
    private readonly IReadOnlyList<string> _messages = options.Value.StatusMessages
        .Where(message => !string.IsNullOrWhiteSpace(message))
        .ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_messages.Count == 0)
        {
            return;
        }

        await WaitUntilReadyAsync(stoppingToken);

        var index = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _messages[index++ % _messages.Count];
            await client.SetActivityAsync(new Game(message));
            logger.LogDebug("Set status to {Status}", message);

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
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
