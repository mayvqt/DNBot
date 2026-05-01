using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace DNBot.Features.Tags;

public sealed class TagStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ConcurrentDictionary<(ulong GuildId, string Name), Tag> _tags = [];
    private readonly object _saveGate = new();
    private readonly string _filePath;

    public TagStore(IHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "data", "tags.json");
        Load();
    }

    public bool TryAdd(Tag tag)
    {
        var added = _tags.TryAdd((tag.GuildId, Normalize(tag.Name)), tag with { Name = Normalize(tag.Name) });
        if (added)
        {
            Save();
        }

        return added;
    }

    public bool TryGet(ulong guildId, string name, out Tag? tag)
    {
        return _tags.TryGetValue((guildId, Normalize(name)), out tag);
    }

    public bool TryRemove(ulong guildId, string name, out Tag? tag)
    {
        var removed = _tags.TryRemove((guildId, Normalize(name)), out tag);
        if (removed)
        {
            Save();
        }

        return removed;
    }

    public IReadOnlyList<Tag> List(ulong guildId)
    {
        return _tags.Values
            .Where(tag => tag.GuildId == guildId)
            .OrderBy(tag => tag.Name)
            .ToArray();
    }

    private static string Normalize(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    private void Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        if (!File.Exists(_filePath))
        {
            return;
        }

        var tags = JsonSerializer.Deserialize<IReadOnlyList<Tag>>(File.ReadAllText(_filePath), JsonOptions) ?? [];
        foreach (var tag in tags)
        {
            _tags[(tag.GuildId, Normalize(tag.Name))] = tag;
        }
    }

    private void Save()
    {
        lock (_saveGate)
        {
            var tags = _tags.Values
                .OrderBy(tag => tag.GuildId)
                .ThenBy(tag => tag.Name)
                .ToArray();

            File.WriteAllText(_filePath, JsonSerializer.Serialize(tags, JsonOptions));
        }
    }
}
