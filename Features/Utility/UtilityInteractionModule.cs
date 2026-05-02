using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DNBot.Features.Utility;

public sealed class UtilityInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("userinfo", "Show information about a user.")]
    public async Task UserInfoAsync(SocketUser? user = null)
    {
        user ??= Context.User;

        var embed = new EmbedBuilder()
            .WithTitle(user.GlobalName ?? user.Username)
            .WithThumbnailUrl(user.GetDisplayAvatarUrl())
            .AddField("User ID", user.Id, true)
            .AddField("Created", TimestampTag.FromDateTimeOffset(user.CreatedAt, TimestampTagStyles.LongDate), true)
            .WithColor(Color.DarkBlue);

        if (user is SocketGuildUser guildUser)
        {
            embed.AddField("Joined", guildUser.JoinedAt is { } joined
                ? TimestampTag.FromDateTimeOffset(joined, TimestampTagStyles.LongDate)
                : "Unknown", true);
            embed.AddField("Top Role",
                guildUser.Roles.OrderByDescending(role => role.Position).FirstOrDefault()?.Mention ?? "None", true);
        }

        await RespondAsync(embed: embed.Build());
    }
}