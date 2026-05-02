using Discord.WebSocket;

namespace DNBot.Features.Levels;

public sealed class MessageRewardService(
    DiscordSocketClient client,
    LevelStore levels,
    ILogger<MessageRewardService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        client.MessageReceived += RewardMessageAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.MessageReceived -= RewardMessageAsync;
        return Task.CompletedTask;
    }

    private Task RewardMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Channel is not SocketGuildChannel guildChannel) return Task.CompletedTask;

        var before = levels.Get(guildChannel.Guild.Id, message.Author.Id);
        var after = levels.AddMessageXp(guildChannel.Guild.Id, message.Author.Id, DateTimeOffset.UtcNow);

        if (after.Level > before.Level)
            logger.LogInformation("User {UserId} reached level {Level} in guild {GuildId}",
                message.Author.Id,
                after.Level,
                guildChannel.Guild.Id);

        return Task.CompletedTask;
    }
}