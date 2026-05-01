using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DNBot.Features.Levels;

[Group("levels", "XP and ranking commands.")]
[RequireContext(ContextType.Guild)]
public sealed class LevelsInteractionModule(LevelStore levels) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("rank", "Show your rank or another user's rank.")]
    public async Task RankAsync(SocketGuildUser? user = null)
    {
        user ??= (SocketGuildUser)Context.User;
        var profile = levels.Get(Context.Guild.Id, user.Id);

        var embed = new EmbedBuilder()
            .WithTitle($"{user.DisplayName}'s rank")
            .WithThumbnailUrl(user.GetDisplayAvatarUrl())
            .AddField("Level", profile.Level, inline: true)
            .AddField("XP", profile.Xp, inline: true)
            .AddField("Next Level", profile.XpForNextLevel, inline: true)
            .WithColor(Color.Gold)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("leaderboard", "Show the top XP earners.")]
    public async Task LeaderboardAsync()
    {
        var top = levels.GetLeaderboard(Context.Guild.Id);
        var description = top.Count == 0
            ? "No XP yet. Send a few messages and check again."
            : string.Join(Environment.NewLine, top.Select((profile, index) =>
                $"{index + 1}. <@{profile.UserId}> - Level {profile.Level} ({profile.Xp} XP)"));

        var embed = new EmbedBuilder()
            .WithTitle("Leaderboard")
            .WithDescription(description)
            .WithColor(Color.Gold)
            .Build();

        await RespondAsync(embed: embed);
    }
}
