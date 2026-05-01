using Discord;
using Discord.Interactions;

namespace DNBot.Features.Polls;

public sealed class PollInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly IEmote[] NumberEmotes =
    [
        new Emoji("1️⃣"),
        new Emoji("2️⃣"),
        new Emoji("3️⃣"),
        new Emoji("4️⃣"),
        new Emoji("5️⃣")
    ];

    [SlashCommand("poll", "Create a reaction poll with two to five options.")]
    public async Task PollAsync(
        [Summary(description: "The question people are voting on.")] string question,
        [Summary(description: "First option.")] string option1,
        [Summary(description: "Second option.")] string option2,
        [Summary(description: "Optional third option.")] string? option3 = null,
        [Summary(description: "Optional fourth option.")] string? option4 = null,
        [Summary(description: "Optional fifth option.")] string? option5 = null)
    {
        var options = new[] { option1, option2, option3, option4, option5 }
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Select(option => option!.Trim())
            .ToArray();

        var description = string.Join(Environment.NewLine, options.Select((option, index) => $"{NumberEmotes[index]} {option}"));
        var embed = new EmbedBuilder()
            .WithTitle(question)
            .WithDescription(description)
            .WithFooter($"Poll by {Context.User.Username}")
            .WithColor(Color.Purple)
            .Build();

        await RespondAsync(embed: embed);
        var response = await GetOriginalResponseAsync();

        foreach (var emote in NumberEmotes.Take(options.Length))
        {
            await response.AddReactionAsync(emote);
        }
    }
}
