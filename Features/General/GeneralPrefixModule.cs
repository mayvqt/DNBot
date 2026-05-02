using Discord;
using Discord.Commands;

namespace DNBot.Features.General;

public sealed class GeneralPrefixModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Check whether the bot is alive.")]
    public async Task PingAsync()
    {
        await ReplyAsync($"Pong! Gateway latency: {Context.Client.Latency}ms");
    }

    [Command("say")]
    [Summary("Repeat a message. Example: !say hello")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task SayAsync([Remainder] string text)
    {
        await Context.Message.DeleteAsync();
        await ReplyAsync(text);
    }
}