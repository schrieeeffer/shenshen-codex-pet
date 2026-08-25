using System.Text.Json;

namespace ShenshenPet.Core;

public sealed class PetSettings
{
    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Scale { get; set; } = 1.0;

    public bool AlwaysOnTop { get; set; } = true;

    public bool AnimationsPaused { get; set; }
}

public static class PetSettingsStore
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ShenshenPet",
        "settings.json");

    public static PetSettings Load(string? path = null)
    {
        var settingsPath = path ?? DefaultPath;
        if (!File.Exists(settingsPath))
        {
            return new PetSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<PetSettings>(File.ReadAllText(settingsPath), JsonOptions)
                ?? new PetSettings();
        }
        catch (JsonException)
        {
            return new PetSettings();
        }
    }

    public static void Save(PetSettings settings, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var settingsPath = path ?? DefaultPath;
        var directory = Path.GetDirectoryName(settingsPath)
            ?? throw new InvalidOperationException("Settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, settingsPath, overwrite: true);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
