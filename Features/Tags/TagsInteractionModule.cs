using Discord;
using Discord.Interactions;

namespace DNBot.Features.Tags;

[Group("tag", "Reusable custom responses.")]
[RequireContext(ContextType.Guild)]
public sealed class TagsInteractionModule(TagStore tags) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create a custom tag.")]
    public async Task CreateAsync(string name, string content)
    {
        if (name.Length is < 2 or > 32)
        {
            await RespondAsync("Tag names must be 2-32 characters.", ephemeral: true);
            return;
        }

        var tag = new Tag(Context.Guild.Id, name, content, Context.User.Id, DateTimeOffset.UtcNow);
        if (!tags.TryAdd(tag))
        {
            await RespondAsync("A tag with that name already exists.", ephemeral: true);
            return;
        }

        await RespondAsync($"Created tag `{name.Trim().ToLowerInvariant()}`.", ephemeral: true);
    }

    [SlashCommand("get", "Post a tag.")]
    public async Task GetAsync(string name)
    {
        if (!tags.TryGet(Context.Guild.Id, name, out var tag) || tag is null)
        {
            await RespondAsync("I could not find that tag.", ephemeral: true);
            return;
        }

        await RespondAsync(tag.Content);
    }

    [SlashCommand("list", "List this server's tags.")]
    public async Task ListAsync()
    {
        var serverTags = tags.List(Context.Guild.Id);
        var description = serverTags.Count == 0
            ? "No tags yet."
            : string.Join(", ", serverTags.Select(tag => $"`{tag.Name}`"));

        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("Tags")
            .WithDescription(description)
            .WithColor(Color.Green)
            .Build(), ephemeral: true);
    }

    [SlashCommand("delete", "Delete a tag you own, or any tag if you can manage messages.")]
    public async Task DeleteAsync(string name)
    {
        if (!tags.TryGet(Context.Guild.Id, name, out var tag) || tag is null)
        {
            await RespondAsync("I could not find that tag.", ephemeral: true);
            return;
        }

        var canManageMessages = Context.User is IGuildUser guildUser
            && guildUser.GuildPermissions.ManageMessages;

        if (tag.OwnerId != Context.User.Id && !canManageMessages)
        {
            await RespondAsync("Only the tag owner or a moderator can delete that tag.", ephemeral: true);
            return;
        }

        tags.TryRemove(Context.Guild.Id, name, out _);
        await RespondAsync($"Deleted tag `{tag.Name}`.", ephemeral: true);
    }
}
