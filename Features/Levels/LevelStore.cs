using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace DNBot.Features.Levels;

public sealed class LevelStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), LevelProfile> _profiles = [];
    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _cooldowns = [];
    private readonly object _saveGate = new();
    private readonly string _filePath;

    public LevelStore(IHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "data", "levels.json");
        Load();
    }

    public LevelProfile AddMessageXp(ulong guildId, ulong userId, DateTimeOffset now)
    {
        var key = (guildId, userId);
        if (_cooldowns.TryGetValue(key, out var lastRewarded) && now - lastRewarded < TimeSpan.FromSeconds(45))
        {
            return Get(guildId, userId);
        }

        _cooldowns[key] = now;
        var xp = Random.Shared.Next(15, 26);

        var profile = _profiles.AddOrUpdate(
            key,
            _ => CreateProfile(guildId, userId, xp),
            (_, profile) => CreateProfile(guildId, userId, profile.Xp + xp));

        Save();
        return profile;
    }

    public LevelProfile Get(ulong guildId, ulong userId)
    {
        return _profiles.GetOrAdd((guildId, userId), _ => new LevelProfile(guildId, userId, 0, 0));
    }

    public IReadOnlyList<LevelProfile> GetLeaderboard(ulong guildId, int limit = 10)
    {
        return _profiles.Values
            .Where(profile => profile.GuildId == guildId)
            .OrderByDescending(profile => profile.Xp)
            .Take(limit)
            .ToArray();
    }

    public void ResetUser(ulong guildId, ulong userId)
    {
        _profiles.TryRemove((guildId, userId), out _);
        _cooldowns.TryRemove((guildId, userId), out _);
        Save();
    }

    public void ResetGuild(ulong guildId)
    {
        foreach (var profile in _profiles.Values.Where(profile => profile.GuildId == guildId))
        {
            _profiles.TryRemove((profile.GuildId, profile.UserId), out _);
            _cooldowns.TryRemove((profile.GuildId, profile.UserId), out _);
        }

        Save();
    }

    public static int XpRequiredForLevel(int level)
    {
        return level <= 0 ? 0 : 100 + (level - 1) * 75;
    }

    private static LevelProfile CreateProfile(ulong guildId, ulong userId, int xp)
    {
        var level = 0;
        while (xp >= XpRequiredForLevel(level + 1))
        {
            level++;
        }

        return new LevelProfile(guildId, userId, xp, level);
    }

    private void Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        if (!File.Exists(_filePath))
        {
            return;
        }

        var profiles = JsonSerializer.Deserialize<IReadOnlyList<LevelProfile>>(File.ReadAllText(_filePath), JsonOptions) ?? [];
        foreach (var profile in profiles)
        {
            _profiles[(profile.GuildId, profile.UserId)] = profile;
        }
    }

    private void Save()
    {
        lock (_saveGate)
        {
            var profiles = _profiles.Values
                .OrderBy(profile => profile.GuildId)
                .ThenByDescending(profile => profile.Xp)
                .ToArray();

            File.WriteAllText(_filePath, JsonSerializer.Serialize(profiles, JsonOptions));
        }
    }
}
