using System.Text.Json;
using Microsoft.Extensions.Hosting;
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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _filePath;
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

    public BotRuntimeSettings Update(string? token, string prefix, ulong? developmentGuildId, IReadOnlyList<string> statusMessages)
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
                ?? new AutoRoleSettings(guildId, Enabled: false, IgnoreBots: true, RoleIds: []);
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

    private BotRuntimeSettings LoadOrCreate(DiscordBotOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (File.Exists(_filePath))
        {
            var loaded = JsonSerializer.Deserialize<BotRuntimeSettings>(File.ReadAllText(_filePath), JsonOptions);
            if (loaded is not null)
            {
                return loaded with
                {
                    StatusMessages = loaded.StatusMessages ?? [],
                    AutoRoles = loaded.AutoRoles ?? []
                };
            }
        }

        var settings = new BotRuntimeSettings(
            options.Token,
            options.Prefix,
            options.DevelopmentGuildId,
            options.StatusMessages.Length == 0 ? DefaultStatusMessages : options.StatusMessages,
            []);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
        return settings;
    }

    private void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_settings, JsonOptions));
    }
}
