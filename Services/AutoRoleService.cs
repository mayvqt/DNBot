using Discord.WebSocket;
using DNBot.Configuration;

namespace DNBot.Services;

public sealed class AutoRoleService(
    DiscordSocketClient client,
    BotSettingsStore settings,
    ILogger<AutoRoleService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        client.UserJoined += AssignRolesAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.UserJoined -= AssignRolesAsync;
        return Task.CompletedTask;
    }

    private async Task AssignRolesAsync(SocketGuildUser user)
    {
        var autoRole = settings.GetAutoRole(user.Guild.Id);
        if (!autoRole.Enabled || autoRole.RoleIds.Count == 0 || (autoRole.IgnoreBots && user.IsBot)) return;

        var roles = autoRole.RoleIds
            .Select(user.Guild.GetRole)
            .Where(role => role is not null)
            .ToArray();

        if (roles.Length == 0)
        {
            logger.LogWarning("Autorole is enabled for guild {GuildId}, but no configured roles exist", user.Guild.Id);
            return;
        }

        try
        {
            await user.AddRolesAsync(roles);
            logger.LogInformation("Assigned {RoleCount} autoroles to user {UserId} in guild {GuildId}",
                roles.Length,
                user.Id,
                user.Guild.Id);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Failed to assign autoroles to user {UserId} in guild {GuildId}",
                user.Id,
                user.Guild.Id);
        }
    }
}