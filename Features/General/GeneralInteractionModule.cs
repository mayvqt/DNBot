using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DNBot.Features.Reminders;

namespace DNBot.Features.General;

public sealed class GeneralInteractionModule(ReminderStore reminders) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Check whether the bot is alive.")]
    public async Task PingAsync()
    {
        var latency = Context.Client.Latency;
        await RespondAsync($"Pong! Gateway latency: {latency}ms", ephemeral: true);
    }

    [SlashCommand("server", "Show a quick server summary.")]
    [RequireContext(ContextType.Guild)]
    public async Task ServerAsync()
    {
        var guild = Context.Guild;
        var embed = new EmbedBuilder()
            .WithTitle(guild.Name)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Members", guild.MemberCount, true)
            .AddField("Channels", guild.Channels.Count, true)
            .AddField("Created", TimestampTag.FromDateTimeOffset(guild.CreatedAt, TimestampTagStyles.LongDate), true)
            .WithColor(Color.Blue)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("avatar", "Get a user's avatar.")]
    public async Task AvatarAsync(SocketUser? user = null)
    {
        user ??= Context.User;

        var avatarUrl = user.GetDisplayAvatarUrl(size: 1024);
        var embed = new EmbedBuilder()
            .WithTitle($"{user.GlobalName ?? user.Username}'s avatar")
            .WithImageUrl(avatarUrl)
            .WithUrl(avatarUrl)
            .WithColor(Color.Teal)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("remind", "Schedule a reminder in this channel.")]
    public async Task RemindAsync(
        [Summary(description: "Delay in minutes.")]
        int minutes,
        [Summary(description: "What should I remind you about?")]
        string text)
    {
        if (minutes is < 1 or > 10080)
        {
            await RespondAsync("Please choose between 1 minute and 7 days.", ephemeral: true);
            return;
        }

        var reminder = reminders.Add(Context.Channel.Id, Context.User.Id, DateTimeOffset.UtcNow.AddMinutes(minutes),
            text);

        await RespondAsync(
            $"Reminder #{reminder.Id} set for {TimestampTag.FromDateTimeOffset(reminder.DueAt, TimestampTagStyles.Relative)}.",
            ephemeral: true);
    }
}