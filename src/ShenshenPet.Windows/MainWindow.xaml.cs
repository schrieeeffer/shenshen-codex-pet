using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ShenshenPet.Core;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace ShenshenPet.Windows;

public partial class MainWindow : Window
{
    private const int MaximumCachedFrames = 12;
    private const string AutoStartValueName = "ShenshenPet";
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private PetManifest _manifest;
    private AnimationPlayer _player;
    private BitmapSource? _atlas;
    private string? _frameDirectory;
    private readonly PetSettings _settings;
    private readonly Dictionary<(int Row, int Column), BitmapSource> _frameCache = [];
    private readonly DispatcherTimer _renderTimer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private NativeTrayIcon? _trayIcon;
    private CodexStateWatcher? _codexStateWatcher;

    private TimeSpan _lastTick;
    private TimeSpan _nextWalkAt;
    private (int Row, int Column)? _renderedCell;
    private Point _dragStartCursor;
    private Point _dragStartWindow;
    private bool _mouseCaptured;
    private bool _dragged;
    private bool _walking;
    private double _walkTarget;
    private int _walkDirection;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _settings = PetSettingsStore.Load();
        NormalizeSettings();

        var activePack = PetPackImporter.TryResolve(_settings.ActivePetPackId);
        if (activePack is null)
        {
            _settings.ActivePetPackId = null;
            (_manifest, _atlas, _frameDirectory) = LoadRuntime(BuiltInManifestPath, BuiltInAtlasPath, BuiltInFramesDirectory);
        }
        else
        {
            (_manifest, _atlas, _frameDirectory) = LoadRuntime(activePack.ManifestPath, activePack.AtlasPath, runtimeFramesDirectory: null);
        }

        _player = new AnimationPlayer(_manifest);
        _settings.Scale = NormalizeScale(_settings.Scale);
        ApplyScale(_settings.Scale);
        ApplyEnergySaverVisuals();
        Topmost = _settings.AlwaysOnTop;
        TopmostMenuItem.IsChecked = Topmost;
        PauseMenuItem.IsChecked = _settings.AnimationsPaused;
        EnergySaverMenuItem.IsChecked = _settings.EnergySaver;
        AutoStartMenuItem.IsChecked = IsAutoStartEnabled();

        _renderTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = GetRenderInterval(),
        };
        _renderTimer.Tick += OnRenderTick;

        UpdateProgressUi();
        UpdateActivePackUi();
        UpdateBridgeMenuState();

        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private static string BuiltInManifestPath => Path.Combine(AppContext.BaseDirectory, "pet", "pet.manifest.json");

    private static string BuiltInAtlasPath => Path.Combine(AppContext.BaseDirectory, "assets", "spritesheet-v2.png");

    private static string BuiltInFramesDirectory => Path.Combine(AppContext.BaseDirectory, "assets", "frames");

    private static (PetManifest Manifest, BitmapSource? Atlas, string? FrameDirectory) LoadRuntime(
        string manifestPath,
        string atlasPath,
        string? runtimeFramesDirectory)
    {
        var manifest = PetManifest.Load(manifestPath);
        if (runtimeFramesDirectory is not null && HasCompleteRuntimeFrames(manifest, runtimeFramesDirectory))
        {
            return (manifest, null, runtimeFramesDirectory);
        }

        return (manifest, LoadBitmap(atlasPath), null);
    }

    private static bool HasCompleteRuntimeFrames(PetManifest manifest, string directory)
    {
        return manifest.Animations.All(animation =>
                Enumerable.Range(0, animation.FrameCount)
                    .All(column => File.Exists(GetRuntimeFramePath(directory, animation.Row, column))))
            && manifest.LookDirections.All(direction =>
                File.Exists(GetRuntimeFramePath(directory, direction.Row, direction.Column)));
    }

    private static string GetRuntimeFramePath(string directory, int row, int column)
    {
        return Path.Combine(directory, $"{row}-{column}.png");
    }

    private static BitmapSource LoadBitmap(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到桌宠精灵表。", path);
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (IsSavedPositionVisible())
        {
            Left = _settings.Left!.Value;
            Top = _settings.Top!.Value;
        }
        else
        {
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Bottom - Height - 24;
        }

        ClampToVirtualScreen();
        _trayIcon = new NativeTrayIcon(this, ShowFromTray, InstallCodexPet, ExitApplication);
        RefreshCodexStateWatcher();
        ScheduleNextWalk();
        _player.Play("waving");
        RenderFrame(force: true);
        UpdateRenderLoop();
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var elapsed = now - _lastTick;
        _lastTick = now;

        UpdateWalking(elapsed, now);
        _player.Advance(elapsed);
        RenderFrame();
    }

    private bool AnimationsReduced => _settings.AnimationsPaused || !SystemParameters.ClientAreaAnimation;

    private TimeSpan GetRenderInterval()
    {
        // Animation frames are 110 ms or longer. The default 10 FPS cadence stays
        // responsive while cutting idle wake-ups by more than 80% versus the old timer.
        return TimeSpan.FromMilliseconds(_settings.EnergySaver ? 100 : 33);
    }

    private void UpdateRenderLoop()
    {
        _renderTimer.Interval = GetRenderInterval();
        if (!IsVisible || AnimationsReduced)
        {
            _renderTimer.Stop();
            RenderFrame(force: true);
            return;
        }

        _lastTick = _clock.Elapsed;
        _renderTimer.Start();
    }

    private void UpdateWalking(TimeSpan elapsed, TimeSpan now)
    {
        if (_mouseCaptured)
        {
            return;
        }

        if (_walking)
        {
            var nextLeft = Left + (_walkDirection * _manifest.Standalone.WalkSpeedPixelsPerSecond * elapsed.TotalSeconds);
            var reachedTarget = _walkDirection > 0 ? nextLeft >= _walkTarget : nextLeft <= _walkTarget;
            Left = reachedTarget ? _walkTarget : nextLeft;
            ClampToVirtualScreen();

            if (reachedTarget)
            {
                _walking = false;
                _player.Play("idle");
                ScheduleNextWalk();
                SaveSettings();
            }

            return;
        }

        if (now < _nextWalkAt || !string.Equals(_player.CurrentAnimation.Id, "idle", StringComparison.Ordinal))
        {
            return;
        }

        var maxLeft = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width;
        var distance = Random.Shared.Next(80, 241);
        var canGoRight = Left + distance <= maxLeft;
        var canGoLeft = Left - distance >= SystemParameters.VirtualScreenLeft;
        if (!canGoLeft && !canGoRight)
        {
            ScheduleNextWalk();
            return;
        }

        _walkDirection = canGoLeft && canGoRight ? (Random.Shared.Next(2) == 0 ? -1 : 1) : (canGoRight ? 1 : -1);
        _walkTarget = Math.Clamp(
            Left + (_walkDirection * distance),
            SystemParameters.VirtualScreenLeft,
            maxLeft);
        _walking = true;
        _player.Play(_walkDirection > 0 ? "running-right" : "running-left");
    }

    private void RenderFrame(bool force = false)
    {
        var cell = AnimationsReduced
            ? (Row: 0, Column: 0)
            : ResolveRenderedCell();

        if (!force && _renderedCell == cell)
        {
            return;
        }

        if (!_frameCache.TryGetValue(cell, out var frame))
        {
            if (_frameDirectory is not null)
            {
                frame = LoadBitmap(GetRuntimeFramePath(_frameDirectory, cell.Row, cell.Column));
                if (frame.PixelWidth != _manifest.Atlas.CellWidth || frame.PixelHeight != _manifest.Atlas.CellHeight)
                {
                    throw new InvalidDataException("运行时帧尺寸与 Pet manifest 不一致。");
                }
            }
            else
            {
                var rectangle = new Int32Rect(
                    cell.Column * _manifest.Atlas.CellWidth,
                    cell.Row * _manifest.Atlas.CellHeight,
                    _manifest.Atlas.CellWidth,
                    _manifest.Atlas.CellHeight);
                var cropped = new CroppedBitmap(
                    _atlas ?? throw new InvalidOperationException("桌宠运行时没有可用精灵表。"),
                    rectangle);
                cropped.Freeze();
                frame = cropped;
            }

            if (_frameCache.Count >= MaximumCachedFrames)
            {
                var evicted = _frameCache.Keys.First(key => key != _renderedCell);
                _frameCache.Remove(evicted);
            }

            _frameCache[cell] = frame;
        }

        PetImage.Source = frame;
        _renderedCell = cell;
    }

    private (int Row, int Column) ResolveRenderedCell()
    {
        if (!_walking
            && !_mouseCaptured
            && string.Equals(_player.CurrentAnimation.Id, "idle", StringComparison.Ordinal)
            && TryResolveLookDirection(out var lookCell))
        {
            return lookCell;
        }

        return (_player.CurrentAnimation.Row, _player.FrameIndex);
    }

    private bool TryResolveLookDirection(out (int Row, int Column) cell)
    {
        cell = default;
        if (!GetCursorPos(out var nativePoint))
        {
            return false;
        }

        var cursor = DeviceToLogical(new Point(nativePoint.X, nativePoint.Y));
        var deltaX = cursor.X - (Left + (ActualWidth / 2));
        var deltaY = cursor.Y - (Top + (ActualHeight / 2));
        var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (distance < _manifest.Standalone.PointerDeadzonePixels
            || distance > _manifest.Standalone.PointerLookRangePixels)
        {
            return false;
        }

        var angle = Math.Atan2(deltaX, -deltaY) * 180 / Math.PI;
        if (angle < 0)
        {
            angle += 360;
        }

        var directionIndex = (int)Math.Round(angle / 22.5, MidpointRounding.AwayFromZero) % 16;
        var direction = _manifest.LookDirections[directionIndex];
        cell = (direction.Row, direction.Column);
        return true;
    }

    private Point DeviceToLogical(Point point)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is { } target
            ? target.TransformFromDevice.Transform(point)
            : point;
    }

    private void OnPetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _walking = false;
        _mouseCaptured = CaptureMouse();
        _dragged = false;
        _dragStartCursor = DeviceToLogical(GetNativeCursorPoint());
        _dragStartWindow = new Point(Left, Top);
        e.Handled = true;
    }

    private void OnPetMouseMove(object sender, MouseEventArgs e)
    {
        if (!_mouseCaptured || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var cursor = DeviceToLogical(GetNativeCursorPoint());
        var deltaX = cursor.X - _dragStartCursor.X;
        var deltaY = cursor.Y - _dragStartCursor.Y;
        if (!_dragged && Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY)) < 4)
        {
            return;
        }

        _dragged = true;
        Left = _dragStartWindow.X + deltaX;
        Top = _dragStartWindow.Y + deltaY;
        ClampToVirtualScreen();
        _player.Play(deltaX >= 0 ? "running-right" : "running-left", restart: false);
        RenderFrame();
        e.Handled = true;
    }

    private void OnPetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !_mouseCaptured)
        {
            return;
        }

        ReleaseMouseCapture();
        _mouseCaptured = false;
        if (_dragged)
        {
            _player.Play("idle");
            SaveSettings();
            ScheduleNextWalk();
        }
        else
        {
            _player.Play("jumping");
        }

        RenderFrame();
        e.Handled = true;
    }

    private static Point GetNativeCursorPoint()
    {
        return GetCursorPos(out var point) ? new Point(point.X, point.Y) : default;
    }

    private void OnClaimRiceClick(object sender, RoutedEventArgs e)
    {
        if (!PetProgress.TryClaimDailyRice(_settings, DateOnly.FromDateTime(DateTime.Now)))
        {
            MessageBox.Show("今天已经领取过白饭啦，明天再来吧。", "深深桌宠", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _player.Play("waving");
        UpdateProgressUi();
        SaveSettings();
    }

    private void OnFeedClick(object sender, RoutedEventArgs e)
    {
        if (!PetProgress.TryFeed(_settings))
        {
            MessageBox.Show("白饭不够了，可以领取今天的白饭。", "深深桌宠", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _player.Play("jumping");
        UpdateProgressUi();
        SaveSettings();
    }

    private void UpdateProgressUi()
    {
        var level = PetProgress.GetBondLevel(_settings);
        ProgressMenuItem.Header = $"白饭 ×{_settings.Rice} · 羁绊 Lv.{level}";
        _trayIcon?.SetTooltip($"深深桌宠 · 白饭 {_settings.Rice} · 羁绊 Lv.{level}");
    }

    private void OnWaveClick(object sender, RoutedEventArgs e) => _player.Play("waving");

    private void OnPreviewStateClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: string state })
        {
            _walking = false;
            _player.Play(state);
        }
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        _settings.AnimationsPaused = PauseMenuItem.IsChecked;
        UpdateRenderLoop();
        SaveSettings();
    }

    private void OnEnergySaverClick(object sender, RoutedEventArgs e)
    {
        _settings.EnergySaver = EnergySaverMenuItem.IsChecked;
        ApplyEnergySaverVisuals();
        UpdateRenderLoop();
        ScheduleNextWalk();
        SaveSettings();
    }

    private void ApplyEnergySaverVisuals()
    {
        RenderOptions.SetBitmapScalingMode(
            PetImage,
            _settings.EnergySaver ? BitmapScalingMode.LowQuality : BitmapScalingMode.HighQuality);
        TryApplyEnergySaverProcessProfile(_settings.EnergySaver);
    }

    private static void TryApplyEnergySaverProcessProfile(bool enabled)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            process.PriorityClass = enabled ? ProcessPriorityClass.BelowNormal : ProcessPriorityClass.Normal;
            var state = new ProcessPowerThrottlingState
            {
                Version = 1,
                ControlMask = 1,
                StateMask = enabled ? 1u : 0u,
            };
            _ = SetProcessInformation(
                process.Handle,
                processInformationClass: 4,
                ref state,
                (uint)Marshal.SizeOf<ProcessPowerThrottlingState>());
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private void OnTopmostClick(object sender, RoutedEventArgs e)
    {
        Topmost = TopmostMenuItem.IsChecked;
        _settings.AlwaysOnTop = Topmost;
        SaveSettings();
    }

    private void OnAutoStartClick(object sender, RoutedEventArgs e)
    {
        try
        {
            SetAutoStart(AutoStartMenuItem.IsChecked);
        }
        catch (Exception exception)
        {
            AutoStartMenuItem.IsChecked = IsAutoStartEnabled();
            MessageBox.Show(exception.Message, "开机启动设置失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnScaleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem { Tag: string value }
            || !double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var scale))
        {
            return;
        }

        _settings.Scale = NormalizeScale(scale);
        ApplyScale(_settings.Scale);
        ClampToVirtualScreen();
        SaveSettings();
    }

    private void ApplyScale(double scale)
    {
        Width = _manifest.Atlas.CellWidth * scale;
        Height = _manifest.Atlas.CellHeight * scale;
        PetImage.Width = Width;
        PetImage.Height = Height;
    }

    private double NormalizeScale(double scale)
    {
        return _manifest.Standalone.SupportedScales
            .OrderBy(candidate => Math.Abs(candidate - scale))
            .First();
    }

    private void OnImportPetPackClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入深深 Pet Pack",
            Filter = "Shenshen Pet Pack (*.zip;*.shenshenpet)|*.zip;*.shenshenpet|ZIP 文件 (*.zip)|*.zip|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var imported = PetPackImporter.Import(dialog.FileName);
            ActivatePet(imported.ManifestPath, imported.AtlasPath, runtimeFramesDirectory: null);
            _settings.ActivePetPackId = imported.Id;
            UpdateActivePackUi();
            SaveSettings();
            MessageBox.Show(
                $"已安全导入并切换到：{imported.DisplayName}\n\n安装目录：{imported.DirectoryPath}",
                "Pet Pack 导入完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Pet Pack 导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnRestoreBuiltInPetClick(object sender, RoutedEventArgs e)
    {
        ActivatePet(BuiltInManifestPath, BuiltInAtlasPath, BuiltInFramesDirectory);
        _settings.ActivePetPackId = null;
        UpdateActivePackUi();
        SaveSettings();
    }

    private void ActivatePet(string manifestPath, string atlasPath, string? runtimeFramesDirectory)
    {
        (_manifest, _atlas, _frameDirectory) = LoadRuntime(manifestPath, atlasPath, runtimeFramesDirectory);
        _player = new AnimationPlayer(_manifest, "waving");
        _frameCache.Clear();
        _renderedCell = null;
        _walking = false;
        _settings.Scale = NormalizeScale(_settings.Scale);
        ApplyScale(_settings.Scale);
        ClampToVirtualScreen();
        ScheduleNextWalk();
        RenderFrame(force: true);
    }

    private void UpdateActivePackUi()
    {
        ActivePackMenuItem.Header = _settings.ActivePetPackId is null
            ? "当前角色：深深（内置）"
            : $"当前角色：{_manifest.DisplayName}（{_settings.ActivePetPackId}）";
    }

    private void OnInstallCodexClick(object sender, RoutedEventArgs e) => InstallCodexPet();

    private void InstallCodexPet()
    {
        try
        {
            var target = CodexPetInstaller.Install(Path.Combine(AppContext.BaseDirectory, "codex"));
            MessageBox.Show(
                $"已安装到：\n{target}\n\n请在设置 > Pets 中刷新并启用“深深”，再输入 /pet 唤醒。",
                "Codex 桌宠安装完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Codex 桌宠安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnInstallCodexBridgeClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var source = Path.Combine(AppContext.BaseDirectory, "codex-bridge", "ShenshenPet.Bridge.exe");
            var result = CodexHookInstaller.Install(source);
            UpdateBridgeMenuState();
            RefreshCodexStateWatcher();
            var backup = result.BackupPath is null ? string.Empty : $"\n原配置备份：{result.BackupPath}";
            MessageBox.Show(
                $"Codex 状态桥接已写入：\n{result.HooksPath}{backup}\n\n请在 Codex CLI 输入 /hooks，检查并信任这些 Hook。桥接只同步预定义动画状态。",
                "Codex 状态桥接已安装",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Codex 状态桥接安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnUninstallCodexBridgeClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var backup = CodexHookInstaller.Uninstall();
            UpdateBridgeMenuState();
            RefreshCodexStateWatcher();
            var backupHint = backup is null ? string.Empty : $"\n卸载前备份：{backup}";
            MessageBox.Show($"已移除深深的 Codex Hook；其他 Hook 保持不变。{backupHint}", "Codex 状态桥接", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Codex 状态桥接卸载失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateBridgeMenuState()
    {
        var installed = CodexHookInstaller.IsInstalled();
        InstallBridgeMenuItem.IsEnabled = !installed;
        UninstallBridgeMenuItem.IsEnabled = installed;
    }

    private void RefreshCodexStateWatcher()
    {
        _codexStateWatcher?.Dispose();
        _codexStateWatcher = null;
        if (!CodexHookInstaller.IsInstalled())
        {
            return;
        }

        _codexStateWatcher = new CodexStateWatcher(state =>
            Dispatcher.BeginInvoke(new Action(() => ApplyCodexState(state)), DispatcherPriority.Background));
    }

    private void ApplyCodexState(string state)
    {
        if (_isExiting)
        {
            return;
        }

        _walking = false;
        _player.Play(state, restart: false);
        if (IsVisible)
        {
            RenderFrame(force: true);
        }
    }

    private void OnHideClick(object sender, RoutedEventArgs e) => HideToTray();

    private void HideToTray()
    {
        SaveSettings();
        _renderTimer.Stop();
        Hide();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        RenderFrame(force: true);
        UpdateRenderLoop();
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => ExitApplication();

    private void ExitApplication()
    {
        _isExiting = true;
        SaveSettings();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        _codexStateWatcher?.Dispose();
        _trayIcon?.Dispose();
    }

    private void ScheduleNextWalk()
    {
        var seconds = _settings.EnergySaver ? Random.Shared.Next(14, 29) : Random.Shared.Next(7, 15);
        _nextWalkAt = _clock.Elapsed + TimeSpan.FromSeconds(seconds);
    }

    private bool IsSavedPositionVisible()
    {
        if (_settings.Left is null || _settings.Top is null)
        {
            return false;
        }

        var right = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        var bottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        return _settings.Left < right
            && _settings.Top < bottom
            && _settings.Left + Width > SystemParameters.VirtualScreenLeft
            && _settings.Top + Height > SystemParameters.VirtualScreenTop;
    }

    private void ClampToVirtualScreen()
    {
        var maxLeft = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width;
        var maxTop = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height;
        Left = Math.Clamp(Left, SystemParameters.VirtualScreenLeft, maxLeft);
        Top = Math.Clamp(Top, SystemParameters.VirtualScreenTop, maxTop);
    }

    private void NormalizeSettings()
    {
        _settings.Rice = Math.Clamp(_settings.Rice, 0, PetProgress.MaximumRice);
        var maximumExperience = (PetProgress.MaximumBondLevel - 1) * PetProgress.ExperiencePerLevel;
        _settings.BondExperience = Math.Clamp(_settings.BondExperience, 0, maximumExperience);
    }

    private void SaveSettings()
    {
        if (!double.IsNaN(Left) && !double.IsNaN(Top))
        {
            _settings.Left = Left;
            _settings.Top = Top;
        }

        PetSettingsStore.Save(_settings);
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        return key?.GetValue(AutoStartValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true)
            ?? throw new InvalidOperationException("无法打开当前用户的启动项设置。");
        if (!enabled)
        {
            key.DeleteValue(AutoStartValueName, throwOnMissingValue: false);
            return;
        }

        var executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定桌宠程序路径。");
        key.SetValue(AutoStartValueName, $"\"{executable}\"");
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessInformation(
        IntPtr process,
        int processInformationClass,
        ref ProcessPowerThrottlingState processInformation,
        uint processInformationSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessPowerThrottlingState
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }
}
