using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShenshenPet.Core;

public sealed class PetManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("unofficial")]
    public bool Unofficial { get; init; }

    [JsonPropertyName("atlas")]
    public AtlasDefinition Atlas { get; init; } = new();

    [JsonPropertyName("animations")]
    public List<AnimationDefinition> Animations { get; init; } = [];

    [JsonPropertyName("lookDirections")]
    public List<LookDirectionDefinition> LookDirections { get; init; } = [];

    [JsonPropertyName("standalone")]
    public StandaloneDefinition Standalone { get; init; } = new();

    public static PetManifest Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<PetManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("The pet manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public AnimationDefinition ResolveAnimation(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var animation = Animations.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase)
            || candidate.Aliases.Any(alias => string.Equals(alias, id, StringComparison.OrdinalIgnoreCase)));
        return animation ?? throw new KeyNotFoundException($"Unknown animation state: {id}");
    }

    public void Validate()
    {
        if (SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported manifest schema version: {SchemaVersion}");
        }

        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidDataException("The pet id and display name are required.");
        }

        if (Atlas.Columns != 8 || Atlas.Rows != 11 || Atlas.CellWidth != 192 || Atlas.CellHeight != 208)
        {
            throw new InvalidDataException("The atlas must use the Codex v2 8x11 grid with 192x208 cells.");
        }

        if (Atlas.Width != Atlas.Columns * Atlas.CellWidth || Atlas.Height != Atlas.Rows * Atlas.CellHeight)
        {
            throw new InvalidDataException("The atlas dimensions do not match its grid.");
        }

        if (Animations.Count != 9)
        {
            throw new InvalidDataException("Exactly nine standard animations are required.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var animation in Animations)
        {
            if (!ids.Add(animation.Id))
            {
                throw new InvalidDataException($"Duplicate animation id: {animation.Id}");
            }

            if (animation.Row is < 0 or > 8)
            {
                throw new InvalidDataException($"Animation row is out of range: {animation.Id}");
            }

            if (animation.FrameCount is < 1 or > 8
                || animation.FrameDurationsMs.Count != animation.FrameCount
                || animation.FrameDurationsMs.Any(duration => duration <= 0))
            {
                throw new InvalidDataException($"Invalid frames or durations for animation: {animation.Id}");
            }
        }

        foreach (var animation in Animations.Where(animation => !string.IsNullOrWhiteSpace(animation.ReturnTo)))
        {
            _ = ResolveAnimation(animation.ReturnTo!);
        }

        if (LookDirections.Count != 16
            || LookDirections.Any(direction => direction.Row is < 9 or > 10 || direction.Column is < 0 or > 7))
        {
            throw new InvalidDataException("Exactly sixteen valid look directions are required.");
        }

        if (!Standalone.SupportedScales.Contains(Standalone.DefaultScale))
        {
            throw new InvalidDataException("The default scale must be included in supported scales.");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };
}

public sealed class AtlasDefinition
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("columns")]
    public int Columns { get; init; }

    [JsonPropertyName("rows")]
    public int Rows { get; init; }

    [JsonPropertyName("cellWidth")]
    public int CellWidth { get; init; }

    [JsonPropertyName("cellHeight")]
    public int CellHeight { get; init; }
}

public sealed class AnimationDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("row")]
    public int Row { get; init; }

    [JsonPropertyName("frameCount")]
    public int FrameCount { get; init; }

    [JsonPropertyName("frameDurationsMs")]
    public List<int> FrameDurationsMs { get; init; } = [];

    [JsonPropertyName("loop")]
    public bool Loop { get; init; }

    [JsonPropertyName("returnTo")]
    public string? ReturnTo { get; init; }
}

public sealed class LookDirectionDefinition
{
    [JsonPropertyName("degrees")]
    public double Degrees { get; init; }

    [JsonPropertyName("row")]
    public int Row { get; init; }

    [JsonPropertyName("column")]
    public int Column { get; init; }
}

public sealed class StandaloneDefinition
{
    [JsonPropertyName("defaultScale")]
    public double DefaultScale { get; init; } = 1.0;

    [JsonPropertyName("supportedScales")]
    public List<double> SupportedScales { get; init; } = [1.0];

    [JsonPropertyName("walkSpeedPixelsPerSecond")]
    public double WalkSpeedPixelsPerSecond { get; init; } = 92.0;

    [JsonPropertyName("pointerDeadzonePixels")]
    public double PointerDeadzonePixels { get; init; } = 56.0;

    [JsonPropertyName("pointerLookRangePixels")]
    public double PointerLookRangePixels { get; init; } = 520.0;
}
