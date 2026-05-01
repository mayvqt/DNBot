using Discord;

namespace DNBot.Configuration;

public sealed class DiscordBotOptions
{
    public const string SectionName = "Discord";

    public string Token { get; init; } = string.Empty;

    public string Prefix { get; init; } = "!";

    public ulong? DevelopmentGuildId { get; init; }

    public LogSeverity DiscordNetLogLevel { get; init; } = LogSeverity.Info;

    public string[] StatusMessages { get; init; } = [];
}
