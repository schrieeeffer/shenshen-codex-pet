using System.IO;
using System.Text.Json;
using ShenshenPet.Core;

namespace ShenshenPet.Windows;

internal sealed class CodexStateWatcher : IDisposable
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.Ordinal)
    {
        "idle",
        "running",
        "waiting",
        "review",
        "failed",
    };

    private readonly Action<string> _stateChanged;
    private readonly FileSystemWatcher _watcher;
    private readonly string _statePath;
    private string? _lastState;

    public CodexStateWatcher(Action<string> stateChanged)
    {
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
        _statePath = Path.Combine(ShenshenDataPaths.DataRoot, "codex-state.json");
        var directory = Path.GetDirectoryName(_statePath)!;
        Directory.CreateDirectory(directory);
        _watcher = new FileSystemWatcher(directory, Path.GetFileName(_statePath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnStateFileChanged;
        _watcher.Created += OnStateFileChanged;
        _watcher.Renamed += OnStateFileRenamed;
        PublishCurrentState();
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnStateFileChanged;
        _watcher.Created -= OnStateFileChanged;
        _watcher.Renamed -= OnStateFileRenamed;
        _watcher.Dispose();
    }

    private void OnStateFileChanged(object sender, FileSystemEventArgs e) => PublishCurrentState();

    private void OnStateFileRenamed(object sender, RenamedEventArgs e) => PublishCurrentState();

    private void PublishCurrentState()
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(_statePath));
            var root = document.RootElement;
            var state = root.GetProperty("state").GetString();
            var updatedAt = root.GetProperty("updatedAt").GetDateTimeOffset();
            if (state is null
                || !AllowedStates.Contains(state)
                || DateTimeOffset.UtcNow - updatedAt > TimeSpan.FromHours(2)
                || string.Equals(state, _lastState, StringComparison.Ordinal))
            {
                return;
            }

            _lastState = state;
            _stateChanged(state);
        }
        catch (IOException)
        {
            // Atomic replacement may briefly race with a file-system notification.
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (JsonException)
        {
        }
        catch (KeyNotFoundException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
