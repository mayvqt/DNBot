using Microsoft.Extensions.Options;

namespace DNBot.Configuration;

public sealed class BotSettingsStore
{
    private static readonly string[] DefaultStatusMessages =
    [
        "/ping",
        "mention me for prefix commands",
        "ready to expand"
    ];

    private readonly string _filePath;

    private readonly object _gate = new();
    private BotRuntimeSettings _settings;

    public BotSettingsStore(IHostEnvironment environment, IOptions<DiscordBotOptions> options)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "data", "settings.json");
        _settings = LoadOrCreate(options.Value);
    }

    public BotRuntimeSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _settings;
            }
        }
    }

    public BotRuntimeSettings Update(string? token, string prefix, ulong? developmentGuildId,
        IReadOnlyList<string> statusMessages)
    {
        var settings = Current;
        var cleaned = settings with
        {
            Token = string.IsNullOrWhiteSpace(token) ? settings.Token : token.Trim(),
            Prefix = string.IsNullOrWhiteSpace(prefix) ? "!" : prefix.Trim()[..Math.Min(prefix.Trim().Length, 8)],
            DevelopmentGuildId = developmentGuildId,
            StatusMessages = statusMessages
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message.Trim())
                .Take(10)
                .ToArray()
        };

        lock (_gate)
        {
            _settings = cleaned;
            Save();
            return _settings;
        }
    }

    public AutoRoleSettings GetAutoRole(ulong guildId)
    {
        lock (_gate)
        {
            return _settings.AutoRoles.FirstOrDefault(settings => settings.GuildId == guildId)
                   ?? new AutoRoleSettings(guildId, false, true, []);
        }
    }

    public AutoRoleSettings UpsertAutoRole(AutoRoleSettings autoRole)
    {
        var cleaned = autoRole with
        {
            RoleIds = autoRole.RoleIds
                .Where(roleId => roleId != 0)
                .Distinct()
                .Take(20)
                .ToArray()
        };

        lock (_gate)
        {
            var autoRoles = _settings.AutoRoles
                .Where(settings => settings.GuildId != cleaned.GuildId)
                .Append(cleaned)
                .OrderBy(settings => settings.GuildId)
                .ToArray();

            _settings = _settings with { AutoRoles = autoRoles };
            Save();
            return cleaned;
        }
    }

    public GuildRuntimeSettings GetGuildSettings(ulong guildId)
    {
        lock (_gate)
        {
            return _settings.GuildSettings.FirstOrDefault(settings => settings.GuildId == guildId)
                   ?? CreateDefaultGuildSettings(guildId);
        }
    }

    public GuildRuntimeSettings UpsertGuildSettings(GuildRuntimeSettings guildSettings)
    {
        var prefix = string.IsNullOrWhiteSpace(guildSettings.PrefixOverride)
            ? null
            : guildSettings.PrefixOverride.Trim()[..Math.Min(guildSettings.PrefixOverride.Trim().Length, 8)];

        var cleaned = guildSettings with
        {
            PrefixOverride = prefix,
            Welcome = CleanWelcome(guildSettings.Welcome)
        };

        lock (_gate)
        {
            var guilds = _settings.GuildSettings
                .Where(settings => settings.GuildId != cleaned.GuildId)
                .Append(cleaned)
                .OrderBy(settings => settings.GuildId)
                .ToArray();

            _settings = _settings with { GuildSettings = guilds };
            Save();
            return cleaned;
        }
    }

    public string GetPrefix(ulong? guildId)
    {
        lock (_gate)
        {
            if (guildId is not { } id) return _settings.Prefix;

            return _settings.GuildSettings.FirstOrDefault(settings => settings.GuildId == id)?.PrefixOverride
                   ?? _settings.Prefix;
        }
    }

    private BotRuntimeSettings LoadOrCreate(DiscordBotOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (File.Exists(_filePath))
        {
            var loaded = JsonFileStore.Read<BotRuntimeSettings>(_filePath);
            if (loaded is not null)
                return loaded with
                {
                    StatusMessages = loaded.StatusMessages ?? [],
                    AutoRoles = loaded.AutoRoles ?? [],
                    GuildSettings = (loaded.GuildSettings ?? [])
                    .Select(settings => settings with
                    {
                        Welcome = CleanWelcome(settings.Welcome)
                    })
                    .ToArray()
                };
        }

        var settings = new BotRuntimeSettings(
            options.Token,
            options.Prefix,
            options.DevelopmentGuildId,
            options.StatusMessages.Length == 0 ? DefaultStatusMessages : options.StatusMessages,
            [],
            []);
        JsonFileStore.Write(_filePath, settings);
        return settings;
    }

    private void Save()
    {
        JsonFileStore.Write(_filePath, _settings);
    }

    private static GuildRuntimeSettings CreateDefaultGuildSettings(ulong guildId)
    {
        return new GuildRuntimeSettings(
            guildId,
            null,
            new WelcomeSettings(
                false,
                null,
                "Welcome {user} to {server}!"));
    }

    private static WelcomeSettings CleanWelcome(WelcomeSettings? welcome)
    {
        if (welcome is null) return new WelcomeSettings(false, null, "Welcome {user} to {server}!");

        var message = string.IsNullOrWhiteSpace(welcome.Message)
            ? "Welcome {user} to {server}!"
            : welcome.Message.Trim();

        return welcome with
        {
            ChannelId = welcome.ChannelId is 0 ? null : welcome.ChannelId,
            Message = message[..Math.Min(message.Length, 500)]
        };
    }
}