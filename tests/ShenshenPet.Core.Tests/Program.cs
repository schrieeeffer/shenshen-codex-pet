using ShenshenPet.Core;

var manifestPath = Path.Combine(AppContext.BaseDirectory, "pet.manifest.json");
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

var testRoot = Path.Combine(Path.GetTempPath(), "shenshen-pet-tests", Guid.NewGuid().ToString("N"));
try
{
    var package = Path.Combine(AppContext.BaseDirectory, "codex");
    var installed = CodexPetInstaller.Install(package, testRoot);
    Expect(File.Exists(Path.Combine(installed, "pet.json")), "Codex pet manifest install");
    Expect(File.Exists(Path.Combine(installed, "spritesheet.webp")), "Codex spritesheet install");
}
finally
{
    if (Directory.Exists(testRoot))
    {
        Directory.Delete(testRoot, recursive: true);
    }
}

Console.WriteLine("OK: core manifest, animation player, and Codex installer tests passed");
return;

static void Expect(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"FAILED: {name}");
    }
}
