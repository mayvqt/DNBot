namespace DNBot.Configuration;

public sealed record BotRuntimeSettings(
    string Token,
    string Prefix,
    ulong? DevelopmentGuildId,
    IReadOnlyList<string> StatusMessages,
    IReadOnlyList<AutoRoleSettings> AutoRoles,
    IReadOnlyList<GuildRuntimeSettings> GuildSettings);

public sealed record AutoRoleSettings(
    ulong GuildId,
    bool Enabled,
    bool IgnoreBots,
    IReadOnlyList<ulong> RoleIds);

public sealed record GuildRuntimeSettings(
    ulong GuildId,
    string? PrefixOverride);
