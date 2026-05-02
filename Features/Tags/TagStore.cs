using System.Collections.Concurrent;
using DNBot.Configuration;

namespace DNBot.Features.Tags;

public sealed class TagStore
{
    private readonly string _filePath;
    private readonly object _saveGate = new();
    private readonly ConcurrentDictionary<(ulong GuildId, string Name), Tag> _tags = [];

    public TagStore(IHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "data", "tags.json");
        Load();
    }

    public bool TryAdd(Tag tag)
    {
        var added = _tags.TryAdd((tag.GuildId, Normalize(tag.Name)), tag with { Name = Normalize(tag.Name) });
        if (added) Save();

        return added;
    }

    public bool TryGet(ulong guildId, string name, out Tag? tag)
    {
        return _tags.TryGetValue((guildId, Normalize(name)), out tag);
    }

    public bool TryRemove(ulong guildId, string name, out Tag? tag)
    {
        var removed = _tags.TryRemove((guildId, Normalize(name)), out tag);
        if (removed) Save();

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
        if (!File.Exists(_filePath)) return;

        var tags = JsonFileStore.Read<IReadOnlyList<Tag>>(_filePath) ?? [];
        foreach (var tag in tags) _tags[(tag.GuildId, Normalize(tag.Name))] = tag;
    }

    private void Save()
    {
        lock (_saveGate)
        {
            var tags = _tags.Values
                .OrderBy(tag => tag.GuildId)
                .ThenBy(tag => tag.Name)
                .ToArray();

            JsonFileStore.Write(_filePath, tags);
        }
    }
}