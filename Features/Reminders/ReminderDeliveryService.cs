using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DNBot.Features.Reminders;

public sealed class ReminderDeliveryService(
    DiscordSocketClient client,
    ReminderStore store,
    ILogger<ReminderDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var reminder in store.TakeDue(DateTimeOffset.UtcNow))
                {
                    await SendReminderAsync(reminder);
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task SendReminderAsync(Reminder reminder)
    {
        if (client.GetChannel(reminder.ChannelId) is not IMessageChannel channel)
        {
            logger.LogWarning("Could not find channel {ChannelId} for reminder {ReminderId}",
                reminder.ChannelId,
                reminder.Id);
            return;
        }

        await channel.SendMessageAsync($"<@{reminder.UserId}> reminder: {reminder.Text}");
    }
}
