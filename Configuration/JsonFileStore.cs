using System.Text.Json;

namespace DNBot.Configuration;

public static class JsonFileStore
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static JsonFileStore()
    {
        Options.Converters.Add(new SnowflakeJsonConverter());
        Options.Converters.Add(new NullableSnowflakeJsonConverter());
    }

    public static T? Read<T>(string path)
    {
        if (!File.Exists(path)) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
        catch (JsonException)
        {
            MoveCorruptFile(path);
            return default;
        }
    }

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(value, Options));

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
            return;
        }

        File.Move(tempPath, path);
    }

    private static void MoveCorruptFile(string path)
    {
        var corruptPath = $"{path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        try
        {
            File.Move(path, corruptPath);
        }
        catch (IOException)
        {
        }
    }
}