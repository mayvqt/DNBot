using Discord;
using Discord.WebSocket;
using DNBot.Configuration;

namespace DNBot.Services;

public sealed class WelcomeService(
    DiscordSocketClient client,
    BotSettingsStore settings,
    ILogger<WelcomeService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        client.UserJoined += SendWelcomeAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.UserJoined -= SendWelcomeAsync;
        return Task.CompletedTask;
    }

    private async Task SendWelcomeAsync(SocketGuildUser user)
    {
        var welcome = settings.GetGuildSettings(user.Guild.Id).Welcome;
        if (!welcome.Enabled || welcome.ChannelId is not { } channelId) return;

        if (client.GetChannel(channelId) is not IMessageChannel channel)
        {
            logger.LogWarning("Welcome channel {ChannelId} was not found in guild {GuildId}", channelId, user.Guild.Id);
            return;
        }

        var message = welcome.Message
            .Replace("{user}", user.Mention, StringComparison.OrdinalIgnoreCase)
            .Replace("{username}", user.Username, StringComparison.OrdinalIgnoreCase)
            .Replace("{server}", user.Guild.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{memberCount}", user.Guild.MemberCount.ToString(), StringComparison.OrdinalIgnoreCase);

        await channel.SendMessageAsync(message);
    }
}