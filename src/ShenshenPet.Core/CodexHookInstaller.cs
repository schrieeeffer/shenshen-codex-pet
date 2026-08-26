using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShenshenPet.Core;

public sealed record CodexHookInstallResult(string HooksPath, string BridgePath, string? BackupPath);

public static class CodexHookInstaller
{
    private const string BridgeFileName = "ShenshenPet.Bridge.exe";
    private const string OwnedStatusMessage = "Shenshen Pet status bridge";

    private static readonly (string EventName, string State)[] EventMappings =
    [
        ("SessionStart", "idle"),
        ("UserPromptSubmit", "running"),
        ("PermissionRequest", "waiting"),
        ("Stop", "review"),
        ("SessionEnd", "idle"),
    ];

    public static string DefaultCodexHome
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("CODEX_HOME");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex")
                : Path.GetFullPath(configured);
        }
    }

    public static string DefaultBridgeRoot => Path.Combine(ShenshenDataPaths.DataRoot, "codex-bridge");

    public static CodexHookInstallResult Install(
        string bridgeSourcePath,
        string? codexHome = null,
        string? bridgeRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bridgeSourcePath);
        if (!File.Exists(bridgeSourcePath))
        {
            throw new FileNotFoundException("发布包缺少 Codex 状态桥接程序。", bridgeSourcePath);
        }

        var targetBridgeRoot = Path.GetFullPath(bridgeRoot ?? DefaultBridgeRoot);
        Directory.CreateDirectory(targetBridgeRoot);
        var targetBridgePath = Path.Combine(targetBridgeRoot, BridgeFileName);
        if (!string.Equals(Path.GetFullPath(bridgeSourcePath), targetBridgePath, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(bridgeSourcePath, targetBridgePath, overwrite: true);
        }

        var sourceConfigPath = bridgeSourcePath + ".config";
        if (File.Exists(sourceConfigPath))
        {
            File.Copy(sourceConfigPath, targetBridgePath + ".config", overwrite: true);
        }

        var targetCodexHome = Path.GetFullPath(codexHome ?? DefaultCodexHome);
        Directory.CreateDirectory(targetCodexHome);
        var hooksPath = Path.Combine(targetCodexHome, "hooks.json");
        var root = LoadHooksRoot(hooksPath);
        var hooks = EnsureHooksObject(root);
        RemoveOwnedHandlers(hooks);

        var command = $"\"{targetBridgePath}\"";
        foreach (var (eventName, state) in EventMappings)
        {
            var groups = EnsureArray(hooks, eventName);
            var handler = new JsonObject
            {
                ["type"] = "command",
                ["command"] = $"{command} {state}",
                ["commandWindows"] = $"{command} {state}",
                ["async"] = true,
                ["timeout"] = 3,
                ["statusMessage"] = OwnedStatusMessage,
            };
            groups.Add(new JsonObject
            {
                ["hooks"] = new JsonArray(handler),
            });
        }

        var backupPath = BackupIfPresent(hooksPath);
        WriteJsonAtomically(hooksPath, root);
        return new CodexHookInstallResult(hooksPath, targetBridgePath, backupPath);
    }

    public static string? Uninstall(string? codexHome = null, string? bridgeRoot = null)
    {
        var hooksPath = Path.Combine(Path.GetFullPath(codexHome ?? DefaultCodexHome), "hooks.json");
        string? backupPath = null;
        if (File.Exists(hooksPath))
        {
            var root = LoadHooksRoot(hooksPath);
            var hooks = EnsureHooksObject(root);
            if (RemoveOwnedHandlers(hooks))
            {
                backupPath = BackupIfPresent(hooksPath);
                WriteJsonAtomically(hooksPath, root);
            }
        }

        var targetBridgePath = Path.Combine(Path.GetFullPath(bridgeRoot ?? DefaultBridgeRoot), BridgeFileName);
        if (File.Exists(targetBridgePath))
        {
            File.Delete(targetBridgePath);
        }

        if (File.Exists(targetBridgePath + ".config"))
        {
            File.Delete(targetBridgePath + ".config");
        }

        return backupPath;
    }

    public static bool IsInstalled(string? codexHome = null)
    {
        var hooksPath = Path.Combine(Path.GetFullPath(codexHome ?? DefaultCodexHome), "hooks.json");
        if (!File.Exists(hooksPath))
        {
            return false;
        }

        try
        {
            var root = LoadHooksRoot(hooksPath);
            return root["hooks"] is JsonObject hooks && ContainsOwnedHandler(hooks);
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static JsonObject LoadHooksRoot(string hooksPath)
    {
        if (!File.Exists(hooksPath))
        {
            return new JsonObject
            {
                ["description"] = "User lifecycle hooks. Shenshen Pet entries are optional and removable.",
            };
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(hooksPath)) as JsonObject
                ?? throw new InvalidDataException("Codex hooks.json 根节点必须是 JSON 对象。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Codex hooks.json 不是有效 JSON；为避免覆盖，安装已停止。", exception);
        }
    }

    private static JsonObject EnsureHooksObject(JsonObject root)
    {
        if (root["hooks"] is null)
        {
            var hooks = new JsonObject();
            root["hooks"] = hooks;
            return hooks;
        }

        return root["hooks"] as JsonObject
            ?? throw new InvalidDataException("Codex hooks.json 的 hooks 字段必须是对象。");
    }

    private static JsonArray EnsureArray(JsonObject hooks, string eventName)
    {
        if (hooks[eventName] is null)
        {
            var groups = new JsonArray();
            hooks[eventName] = groups;
            return groups;
        }

        return hooks[eventName] as JsonArray
            ?? throw new InvalidDataException($"Codex hooks.json 的 {eventName} 字段必须是数组。");
    }

    private static bool ContainsOwnedHandler(JsonObject hooks)
    {
        return hooks.Any(property =>
            property.Value is JsonArray groups
            && groups.OfType<JsonObject>().Any(GroupContainsOwnedHandler));
    }

    private static bool RemoveOwnedHandlers(JsonObject hooks)
    {
        var changed = false;
        foreach (var property in hooks.ToList())
        {
            if (property.Value is not JsonArray groups)
            {
                continue;
            }

            for (var groupIndex = groups.Count - 1; groupIndex >= 0; groupIndex--)
            {
                if (groups[groupIndex] is not JsonObject group || group["hooks"] is not JsonArray handlers)
                {
                    continue;
                }

                for (var handlerIndex = handlers.Count - 1; handlerIndex >= 0; handlerIndex--)
                {
                    if (handlers[handlerIndex] is JsonObject handler && IsOwnedHandler(handler))
                    {
                        handlers.RemoveAt(handlerIndex);
                        changed = true;
                    }
                }

                if (handlers.Count == 0)
                {
                    groups.RemoveAt(groupIndex);
                }
            }

            if (groups.Count == 0)
            {
                hooks.Remove(property.Key);
            }
        }

        return changed;
    }

    private static bool GroupContainsOwnedHandler(JsonObject group)
    {
        return group["hooks"] is JsonArray handlers
            && handlers.OfType<JsonObject>().Any(IsOwnedHandler);
    }

    private static bool IsOwnedHandler(JsonObject handler)
    {
        return string.Equals(GetString(handler["statusMessage"]), OwnedStatusMessage, StringComparison.Ordinal)
            || GetString(handler["commandWindows"])?.Contains(BridgeFileName, StringComparison.OrdinalIgnoreCase) == true
            || GetString(handler["command"])?.Contains(BridgeFileName, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetString(JsonNode? node)
    {
        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    }

    private static string? BackupIfPresent(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var backupPath = $"{path}.shenshen-backup-{DateTime.Now:yyyyMMdd-HHmmssfff}";
        File.Copy(path, backupPath, overwrite: false);
        return backupPath;
    }

    private static void WriteJsonAtomically(string path, JsonObject root)
    {
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, root.ToJsonString(JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };
}
