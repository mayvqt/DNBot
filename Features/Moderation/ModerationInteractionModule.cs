using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DNBot.Features.Moderation;

[Group("mod", "Moderation tools.")]
[RequireContext(ContextType.Guild)]
public sealed class ModerationInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("kick", "Kick a member from the server.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    public async Task KickAsync(SocketGuildUser user, string reason = "No reason provided.")
    {
        await user.KickAsync(reason);
        await RespondAsync($"Kicked {user.Mention}. Reason: {reason}");
    }

    [SlashCommand("ban", "Ban a member from the server.")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(SocketGuildUser user, string reason = "No reason provided.")
    {
        await user.BanAsync(pruneDays: 0, reason);
        await RespondAsync($"Banned {user.Mention}. Reason: {reason}");
    }

    [SlashCommand("purge", "Bulk-delete recent messages from this channel.")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task PurgeAsync([MinValue(1), MaxValue(100)] int count)
    {
        if (Context.Channel is not ITextChannel textChannel)
        {
            await RespondAsync("This command only works in text channels.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var messages = await textChannel.GetMessagesAsync(count).FlattenAsync();
        var recentMessages = messages
            .Where(message => DateTimeOffset.UtcNow - message.CreatedAt < TimeSpan.FromDays(14))
            .ToArray();

        await textChannel.DeleteMessagesAsync(recentMessages);
        await FollowupAsync($"Deleted {recentMessages.Length} messages.", ephemeral: true);
    }

    [SlashCommand("slowmode", "Set channel slowmode in seconds.")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    public async Task SlowmodeAsync([MinValue(0), MaxValue(21600)] int seconds)
    {
        if (Context.Channel is not SocketTextChannel channel)
        {
            await RespondAsync("This command only works in text channels.", ephemeral: true);
            return;
        }

        await channel.ModifyAsync(properties => properties.SlowModeInterval = seconds);
        await RespondAsync(seconds == 0 ? "Slowmode disabled." : $"Slowmode set to {seconds} seconds.");
    }
}
