using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace ShenshenPet.Bridge;

internal static class Program
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.Ordinal)
    {
        "idle",
        "running",
        "waiting",
        "review",
        "failed",
    };

    private static string DefaultStatePath
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("SHENSHEN_DATA_HOME");
            var dataRoot = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShenshenPet")
                : Path.GetFullPath(configured);
            return Path.Combine(dataRoot, "codex-state.json");
        }
    }

    public static int Main(string[] args)
    {
        if (args.Any(argument => string.Equals(argument, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSelfTest();
        }

        // Codex sends the hook payload on stdin. Drain it without parsing or persisting
        // prompts/transcripts: the bridge only needs the explicit state argument.
        _ = Console.In.ReadToEnd();
        if (args.Length == 0 || !AllowedStates.Contains(args[0]))
        {
            return 0;
        }

        try
        {
            WriteState(DefaultStatePath, args[0]);
        }
        catch
        {
            // A decorative pet must never block or fail a Codex turn.
        }

        return 0;
    }

    private static int RunSelfTest()
    {
        var directory = Path.Combine(Path.GetTempPath(), "shenshen-bridge-test", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "state.json");
        try
        {
            WriteState(path, "running");
            return File.ReadAllText(path).IndexOf("\"state\":\"running\"", StringComparison.Ordinal) >= 0 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static void WriteState(string path, string state)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("State path has no parent directory.");
        Directory.CreateDirectory(directory);
        var updatedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var json = $"{{\"state\":\"{state}\",\"updatedAt\":\"{updatedAt}\",\"source\":\"codex-hook\"}}";
        var temporaryPath = path + $".{Process.GetCurrentProcess().Id}.tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        if (File.Exists(path))
        {
            File.Replace(temporaryPath, path, null);
        }
        else
        {
            File.Move(temporaryPath, path);
        }
    }
}
