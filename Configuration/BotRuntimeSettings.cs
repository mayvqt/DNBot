namespace DNBot.Configuration;

public sealed record BotRuntimeSettings(
    string Token,
    string Prefix,
    ulong? DevelopmentGuildId,
    IReadOnlyList<string> StatusMessages,
    IReadOnlyList<AutoRoleSettings> AutoRoles);

public sealed record AutoRoleSettings(
    ulong GuildId,
    bool Enabled,
    bool IgnoreBots,
    IReadOnlyList<ulong> RoleIds);
