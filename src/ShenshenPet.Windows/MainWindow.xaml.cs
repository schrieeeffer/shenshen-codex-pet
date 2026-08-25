using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ShenshenPet.Core;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace ShenshenPet.Windows;

public partial class MainWindow : Window
{
    private const string AutoStartValueName = "ShenshenPet";
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly PetManifest _manifest;
    private readonly AnimationPlayer _player;
    private readonly PetSettings _settings;
    private readonly BitmapSource _atlas;
    private readonly Dictionary<(int Row, int Column), BitmapSource> _frameCache = [];
    private readonly DispatcherTimer _renderTimer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Forms.NotifyIcon _trayIcon;

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

        var manifestPath = Path.Combine(AppContext.BaseDirectory, "pet", "pet.manifest.json");
        var atlasPath = Path.Combine(AppContext.BaseDirectory, "assets", "spritesheet-v2.png");
        _manifest = PetManifest.Load(manifestPath);
        _player = new AnimationPlayer(_manifest);
        _settings = PetSettingsStore.Load();
        _atlas = LoadBitmap(atlasPath);

        _settings.Scale = NormalizeScale(_settings.Scale);
        ApplyScale(_settings.Scale);
        Topmost = _settings.AlwaysOnTop;
        TopmostMenuItem.IsChecked = Topmost;
        PauseMenuItem.IsChecked = _settings.AnimationsPaused;
        AutoStartMenuItem.IsChecked = IsAutoStartEnabled();

        _trayIcon = CreateTrayIcon();
        _renderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _renderTimer.Tick += OnRenderTick;

        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
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

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示深深", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("安装到 Codex", null, (_, _) => Dispatcher.Invoke(InstallCodexPet));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Dispatcher.Invoke(ExitApplication));

        var trayIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "深深桌宠",
            ContextMenuStrip = menu,
            Visible = true,
        };
        trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
        return trayIcon;
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
        ScheduleNextWalk();
        RenderFrame(force: true);
        _lastTick = _clock.Elapsed;
        _renderTimer.Start();
        _player.Play("waving");
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var elapsed = now - _lastTick;
        _lastTick = now;

        if (!AnimationsReduced)
        {
            UpdateWalking(elapsed, now);
            _player.Advance(elapsed);
        }

        RenderFrame();
    }

    private bool AnimationsReduced => _settings.AnimationsPaused || !SystemParameters.ClientAreaAnimation;

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
            var rectangle = new Int32Rect(
                cell.Column * _manifest.Atlas.CellWidth,
                cell.Row * _manifest.Atlas.CellHeight,
                _manifest.Atlas.CellWidth,
                _manifest.Atlas.CellHeight);
            var cropped = new CroppedBitmap(_atlas, rectangle);
            cropped.Freeze();
            frame = cropped;
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

        e.Handled = true;
    }

    private static Point GetNativeCursorPoint()
    {
        if (!GetCursorPos(out var point))
        {
            return default;
        }

        return new Point(point.X, point.Y);
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
        RenderFrame(force: true);
        SaveSettings();
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

    private void OnHideClick(object sender, RoutedEventArgs e) => Hide();

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
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
        Hide();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private void ScheduleNextWalk()
    {
        _nextWalkAt = _clock.Elapsed + TimeSpan.FromSeconds(Random.Shared.Next(7, 15));
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
