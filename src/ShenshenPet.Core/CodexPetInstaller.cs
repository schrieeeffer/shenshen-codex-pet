namespace ShenshenPet.Core;

public static class CodexPetInstaller
{
    private static readonly string[] RequiredFiles = ["pet.json", "spritesheet.webp"];

    public static string ResolveCodexHome()
    {
        var configured = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
    }

    public static string Install(string packageDirectory, string? codexHome = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);
        var source = Path.GetFullPath(packageDirectory);
        foreach (var file in RequiredFiles)
        {
            var sourceFile = Path.Combine(source, file);
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException($"The Codex pet package is missing {file}.", sourceFile);
            }
        }

        var root = Path.GetFullPath(codexHome ?? ResolveCodexHome());
        var petsRoot = Path.Combine(root, "pets");
        var target = Path.Combine(petsRoot, "shenshen");
        Directory.CreateDirectory(target);

        foreach (var file in RequiredFiles)
        {
            File.Copy(Path.Combine(source, file), Path.Combine(target, file), overwrite: true);
        }

        return target;
    }
}
