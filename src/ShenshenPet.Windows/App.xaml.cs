using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShenshenPet.Core;
using MessageBox = System.Windows.MessageBox;

namespace ShenshenPet.Windows;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\ShenshenPet.Desktop";

    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(argument => string.Equals(argument, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                RunSelfTest();
                Shutdown(0);
            }
            catch
            {
                Shutdown(1);
            }

            return;
        }

        if (e.Args.Any(argument => string.Equals(argument, "--install-codex", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var target = CodexPetInstaller.Install(Path.Combine(AppContext.BaseDirectory, "codex"));
                MessageBox.Show(
                    $"已安装深深 Codex 桌宠：\n{target}\n\n请在设置 > Pets 中刷新并启用，然后输入 /pet 唤醒。",
                    "深深桌宠",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            Shutdown();
            return;
        }

        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            MessageBox.Show("深深桌宠已经在运行。", "深深桌宠", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            ShowFatalError(exception);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;

        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ShowFatalError(e.Exception);
        Shutdown(1);
    }

    private static void ShowFatalError(Exception exception)
    {
        var logPath = TryWriteCrashLog(exception);
        var logHint = logPath is null ? string.Empty : $"\n\n诊断日志：{logPath}";
        MessageBox.Show(
            $"桌宠启动或运行时遇到错误：\n{exception.Message}\n\n请确认已完整解压发布包后再运行。{logHint}",
            "深深桌宠运行失败",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string? TryWriteCrashLog(Exception exception)
    {
        try
        {
            var directory = ShenshenDataPaths.DataRoot;
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "crash.log");
            File.AppendAllText(path, $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static void RunSelfTest()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "pet", "pet.manifest.json");
        var atlasPath = Path.Combine(AppContext.BaseDirectory, "assets", "spritesheet-v2.png");
        var codexManifestPath = Path.Combine(AppContext.BaseDirectory, "codex", "pet.json");
        var codexAtlasPath = Path.Combine(AppContext.BaseDirectory, "codex", "spritesheet.webp");
        var manifest = PetManifest.Load(manifestPath);
        EnsureNonEmptyFile(atlasPath);
        EnsureNonEmptyFile(codexManifestPath);
        EnsureNonEmptyFile(codexAtlasPath);

        using (var atlasStream = File.OpenRead(atlasPath))
        {
            var actualHash = Convert.ToHexString(SHA256.HashData(atlasStream)).ToLowerInvariant();
            if (!string.Equals(actualHash, manifest.Atlas.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("桌宠精灵表校验失败。");
            }

            atlasStream.Position = 0;
            var decoder = BitmapDecoder.Create(atlasStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.Single();
            if (frame.PixelWidth != manifest.Atlas.Width || frame.PixelHeight != manifest.Atlas.Height)
            {
                throw new InvalidDataException("桌宠精灵表尺寸与清单不一致。");
            }
        }

        using var codexManifest = JsonDocument.Parse(File.ReadAllText(codexManifestPath));
        var root = codexManifest.RootElement;
        if (root.GetProperty("spriteVersionNumber").GetInt32() != 2
            || !string.Equals(root.GetProperty("spritesheetPath").GetString(), "spritesheet.webp", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Codex 桌宠清单无效。");
        }
    }

    private static void EnsureNonEmptyFile(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            throw new FileNotFoundException("发布包缺少必需文件。", path);
        }
    }
}
