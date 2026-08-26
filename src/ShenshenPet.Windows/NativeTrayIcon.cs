using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ShenshenPet.Windows;

internal sealed class NativeTrayIcon : IDisposable
{
    private const uint NotifyIconId = 1;
    private const uint CallbackMessage = 0x8000 + 0x52;
    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NotifyIconVersion4 = 4;
    private const uint WmNull = 0x0000;
    private const uint WmContextMenu = 0x007B;
    private const uint WmLeftButtonUp = 0x0202;
    private const uint WmLeftButtonDoubleClick = 0x0203;
    private const uint WmRightButtonUp = 0x0205;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0401;
    private const uint MfString = 0x00000000;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmReturnCommand = 0x0100;
    private const uint TpmNonotify = 0x0080;
    private const uint CommandShow = 1001;
    private const uint CommandInstallCodex = 1002;
    private const uint CommandExit = 1003;
    private static readonly IntPtr IdiApplication = new(32512);

    private readonly HwndSource _source;
    private readonly Action _show;
    private readonly Action _installCodex;
    private readonly Action _exit;
    private NotifyIconData _data;
    private bool _disposed;

    public NativeTrayIcon(Window owner, Action show, Action installCodex, Action exit)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _show = show ?? throw new ArgumentNullException(nameof(show));
        _installCodex = installCodex ?? throw new ArgumentNullException(nameof(installCodex));
        _exit = exit ?? throw new ArgumentNullException(nameof(exit));

        var handle = new WindowInteropHelper(owner).Handle;
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法取得桌宠窗口句柄。");
        }

        _source = HwndSource.FromHwnd(handle)
            ?? throw new InvalidOperationException("无法连接桌宠窗口消息循环。");
        _source.AddHook(WndProc);

        _data = CreateData(handle, "深深桌宠");
        if (!ShellNotifyIcon(NimAdd, ref _data))
        {
            _source.RemoveHook(WndProc);
            throw new InvalidOperationException("无法创建系统托盘图标。");
        }

        _data.VersionOrTimeout = NotifyIconVersion4;
        _ = ShellNotifyIcon(NimSetVersion, ref _data);
    }

    public void SetTooltip(string tooltip)
    {
        if (_disposed)
        {
            return;
        }

        _data.Tooltip = string.IsNullOrWhiteSpace(tooltip) ? "深深桌宠" : tooltip[..Math.Min(tooltip.Length, 127)];
        _data.Flags = NifMessage | NifIcon | NifTip;
        _ = ShellNotifyIcon(NimModify, ref _data);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = ShellNotifyIcon(NimDelete, ref _data);
        _source.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)message != CallbackMessage)
        {
            return IntPtr.Zero;
        }

        var mouseMessage = (uint)(lParam.ToInt64() & 0xFFFF);
        if (mouseMessage is WmLeftButtonUp or WmLeftButtonDoubleClick or NinSelect or NinKeySelect)
        {
            handled = true;
            _show();
        }
        else if (mouseMessage is WmRightButtonUp or WmContextMenu)
        {
            handled = true;
            ShowContextMenu(hwnd);
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu(IntPtr hwnd)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            _ = AppendMenu(menu, MfString, CommandShow, "显示深深");
            _ = AppendMenu(menu, MfString, CommandInstallCodex, "安装到 Codex");
            _ = AppendMenu(menu, MfSeparator, 0, null);
            _ = AppendMenu(menu, MfString, CommandExit, "退出");
            if (!GetCursorPos(out var cursor))
            {
                return;
            }

            _ = SetForegroundWindow(hwnd);
            var command = TrackPopupMenu(
                menu,
                TpmRightButton | TpmReturnCommand | TpmNonotify,
                cursor.X,
                cursor.Y,
                hwnd,
                IntPtr.Zero);
            switch (command)
            {
                case CommandShow:
                    _show();
                    break;
                case CommandInstallCodex:
                    _installCodex();
                    break;
                case CommandExit:
                    _exit();
                    break;
            }

            _ = PostMessage(hwnd, WmNull, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            _ = DestroyMenu(menu);
        }
    }

    private static NotifyIconData CreateData(IntPtr handle, string tooltip)
    {
        return new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = handle,
            Id = NotifyIconId,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = CallbackMessage,
            IconHandle = LoadIcon(IntPtr.Zero, IdiApplication),
            Tooltip = tooltip,
            Info = string.Empty,
            InfoTitle = string.Empty,
        };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public IntPtr WindowHandle;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr IconHandle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tooltip;

        public uint State;
        public uint StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;

        public uint VersionOrTimeout;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;

        public uint InfoFlags;
        public Guid GuidItem;
        public IntPtr BalloonIconHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menu, uint flags, uint itemId, string? text);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll", EntryPoint = "TrackPopupMenuEx")]
    private static extern uint TrackPopupMenu(
        IntPtr menu,
        uint flags,
        int x,
        int y,
        IntPtr owner,
        IntPtr parameters);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);
}
