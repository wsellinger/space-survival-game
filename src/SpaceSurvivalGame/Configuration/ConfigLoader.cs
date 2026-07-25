using System.IO;
using System.Text.Json;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// Shared JSON load-or-create-defaults logic for the *Config classes: read a JSON file next to
/// the executable if present, else write out a default instance so there's always something to
/// open and tweak.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static T Load<T>(string path) where T : new()
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(path));
            if (loaded != null) return loaded;
        }

        var defaultConfig = new T();
        File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, SerializerOptions));
        return defaultConfig;
    }
}
