using System.IO.Compression;
using System.Text.Json;
using ShenshenPet.Core;

var manifestPath = Path.Combine(AppContext.BaseDirectory, "pet.manifest.json");
var atlasPath = Path.Combine(AppContext.BaseDirectory, "assets", "spritesheet-v2.png");
var manifest = PetManifest.Load(manifestPath);

Expect(manifest.Id == "shenshen", "manifest id");
Expect(manifest.ResolveAnimation("active-work").Id == "running", "active-work compatibility alias");
Expect(manifest.LookDirections.Count == 16, "look direction count");

var player = new AnimationPlayer(manifest);
Expect(player.CurrentAnimation.Id == "idle" && player.FrameIndex == 0, "initial animation");
player.Advance(TimeSpan.FromMilliseconds(281));
Expect(player.FrameIndex == 1, "idle frame advance");

player.Play("waving");
player.Advance(TimeSpan.FromMilliseconds(701));
Expect(player.CurrentAnimation.Id == "idle" && player.FrameIndex == 0, "one-shot return to idle");

var progress = new PetSettings { Rice = 0 };
var today = new DateOnly(2026, 8, 26);
Expect(PetProgress.TryClaimDailyRice(progress, today), "first daily rice claim");
Expect(progress.Rice == 3, "daily rice reward");
Expect(!PetProgress.TryClaimDailyRice(progress, today), "duplicate daily rice claim rejected");
Expect(PetProgress.TryFeed(progress), "feed with rice");
Expect(progress.Rice == 2 && progress.BondExperience == 1, "feed consumes rice and gains bond xp");
for (var index = 0; index < 4; index++)
{
    progress.Rice++;
    Expect(PetProgress.TryFeed(progress), $"feed for level progress {index}");
}

Expect(PetProgress.GetBondLevel(progress) == 2, "bond level calculation");

var testRoot = Path.Combine(Path.GetTempPath(), "shenshen-pet-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);
try
{
    TestDataHomeOverride(testRoot);
    TestCodexPetInstall(testRoot);
    TestPetPackImport(testRoot, manifestPath, atlasPath, manifest.Atlas.Sha256);
    TestPetPackTraversalRejection(testRoot);
    TestPetPackHashRejection(testRoot, manifestPath, atlasPath, manifest.Atlas.Sha256);
    TestCodexHookInstallAndUninstall(testRoot);
}
finally
{
    if (Directory.Exists(testRoot))
    {
        Directory.Delete(testRoot, recursive: true);
    }
}

Console.WriteLine("OK: core manifest, animation, progress, Pet Pack, Codex pet, and Hook installer tests passed");
return;

static void TestDataHomeOverride(string testRoot)
{
    var previous = Environment.GetEnvironmentVariable(ShenshenDataPaths.DataHomeEnvironmentVariable);
    var expected = Path.Combine(testRoot, "isolated data home");
    try
    {
        Environment.SetEnvironmentVariable(ShenshenDataPaths.DataHomeEnvironmentVariable, expected);
        Expect(ShenshenDataPaths.DataRoot == Path.GetFullPath(expected), "data home environment override");
        Expect(PetSettingsStore.DefaultPath.StartsWith(Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "settings use data home override");
        Expect(PetPackImporter.DefaultPacksRoot.StartsWith(Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase), "Pet Pack uses data home override");
    }
    finally
    {
        Environment.SetEnvironmentVariable(ShenshenDataPaths.DataHomeEnvironmentVariable, previous);
    }
}

static void TestCodexPetInstall(string testRoot)
{
    var package = Path.Combine(AppContext.BaseDirectory, "codex");
    var installed = CodexPetInstaller.Install(package, Path.Combine(testRoot, "codex-pet-home"));
    Expect(File.Exists(Path.Combine(installed, "pet.json")), "Codex pet manifest install");
    Expect(File.Exists(Path.Combine(installed, "spritesheet.webp")), "Codex spritesheet install");
}

static void TestPetPackImport(
    string testRoot,
    string manifestPath,
    string atlasPath,
    string atlasHash)
{
    var packagePath = Path.Combine(testRoot, "valid-pack.zip");
    CreatePack(packagePath, File.ReadAllText(manifestPath), atlasPath);
    var packsRoot = Path.Combine(testRoot, "packs");
    var imported = PetPackImporter.Import(packagePath, packsRoot);
    Expect(imported.Id == "shenshen", "Pet Pack id");
    Expect(File.Exists(imported.ManifestPath) && File.Exists(imported.AtlasPath), "Pet Pack extracted files");
    Expect(PetPackImporter.TryResolve("shenshen", packsRoot)?.AtlasPath == imported.AtlasPath, "Pet Pack resolve");
    Expect(PetManifest.Load(imported.ManifestPath).Atlas.Sha256 == atlasHash, "Pet Pack manifest preserved");
}

static void TestPetPackTraversalRejection(string testRoot)
{
    var packagePath = Path.Combine(testRoot, "traversal-pack.zip");
    using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
    {
        using var writer = new StreamWriter(archive.CreateEntry("../escape.txt").Open());
        writer.Write("blocked");
    }

    ExpectThrows<InvalidDataException>(
        () => PetPackImporter.Import(packagePath, Path.Combine(testRoot, "traversal-packs")),
        "Pet Pack directory traversal rejection");
    Expect(!File.Exists(Path.Combine(testRoot, "escape.txt")), "Pet Pack did not escape destination");
}

static void TestPetPackHashRejection(
    string testRoot,
    string manifestPath,
    string atlasPath,
    string atlasHash)
{
    var packagePath = Path.Combine(testRoot, "bad-hash-pack.zip");
    var invalidManifest = File.ReadAllText(manifestPath)
        .Replace(atlasHash, new string('0', 64), StringComparison.Ordinal);
    CreatePack(packagePath, invalidManifest, atlasPath);
    ExpectThrows<InvalidDataException>(
        () => PetPackImporter.Import(packagePath, Path.Combine(testRoot, "bad-hash-packs")),
        "Pet Pack hash mismatch rejection");
}

static void TestCodexHookInstallAndUninstall(string testRoot)
{
    var fakeBridge = Path.Combine(testRoot, "ShenshenPet.Bridge.exe");
    File.WriteAllText(fakeBridge, "test bridge");
    File.WriteAllText(fakeBridge + ".config", "test config");
    var codexHome = Path.Combine(testRoot, "codex-hooks-home");
    var bridgeRoot = Path.Combine(testRoot, "bridge install with spaces");
    Directory.CreateDirectory(codexHome);
    var hooksPath = Path.Combine(codexHome, "hooks.json");
    File.WriteAllText(
        hooksPath,
        "{\"hooks\":{\"Stop\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"existing.exe\"}]}]}}");

    var installed = CodexHookInstaller.Install(fakeBridge, codexHome, bridgeRoot);
    Expect(File.Exists(installed.BridgePath), "Codex Hook bridge copied");
    Expect(File.Exists(installed.BridgePath + ".config"), "Codex Hook bridge config copied");
    Expect(installed.BackupPath is not null && File.Exists(installed.BackupPath), "Codex Hook backup created");
    Expect(CodexHookInstaller.IsInstalled(codexHome), "Codex Hook installed marker");
    using (var document = JsonDocument.Parse(File.ReadAllText(hooksPath)))
    {
        Expect(document.RootElement.ToString().Contains("existing.exe", StringComparison.Ordinal), "existing Codex Hook preserved");
        Expect(document.RootElement.ToString().Contains("bridge install with spaces", StringComparison.Ordinal), "Codex Hook quoted path preserved");
    }

    _ = CodexHookInstaller.Uninstall(codexHome, bridgeRoot);
    Expect(!CodexHookInstaller.IsInstalled(codexHome), "Codex Hook uninstall marker removed");
    Expect(!File.Exists(installed.BridgePath), "Codex Hook bridge removed");
    Expect(!File.Exists(installed.BridgePath + ".config"), "Codex Hook bridge config removed");
    Expect(File.ReadAllText(hooksPath).Contains("existing.exe", StringComparison.Ordinal), "existing Hook preserved after uninstall");
}

static void CreatePack(string packagePath, string manifestJson, string atlasPath)
{
    using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
    using (var writer = new StreamWriter(archive.CreateEntry("pet.manifest.json", CompressionLevel.Optimal).Open()))
    {
        writer.Write(manifestJson);
    }

    archive.CreateEntryFromFile(atlasPath, "assets/spritesheet-v2.png", CompressionLevel.Optimal);
}

static void Expect(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"FAILED: {name}");
    }
}

static void ExpectThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"FAILED: {name}");
}
