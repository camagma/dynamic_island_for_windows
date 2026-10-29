using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using static dynamic_island_prototype.Platform.NativeMethods;

namespace dynamic_island_prototype.Platform;

// All Win32 coordinates are physical pixels; WPF dimensions remain device independent.
internal sealed class IslandWindowBehavior : IDisposable
{
    private readonly Window _window;
    private readonly nint _handle;
    private readonly HwndSource _source;
    private readonly DispatcherTimer _timer;
    private bool _hidden;
    private double _lastScale;
    private readonly Action<bool> _reveal;
    private readonly Func<bool> _keepOpen;
    private bool _revealed;
    private long _lastInside;
    public IslandWindowBehavior(Window window, Action<bool> reveal, Func<bool> keepOpen)
    {
        _window = window;
        _reveal = reveal;
        _keepOpen = keepOpen;
        _handle = new WindowInteropHelper(window).Handle;
        SetWindowLong(_handle, -20, (GetWindowLong(_handle, -20) | 0x08000000 | 0x80) & ~0x40000);
        _source = HwndSource.FromHwnd(_handle)!;
        _source.AddHook(Hook);
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Background, Tick, window.Dispatcher);
        Place();
    }
    private nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x21) { handled = true; return 3; } // MA_NOACTIVATE preserves button clicks.
        if (message is 0x02E0 or 0x007E or 0x001A)
            _window.Dispatcher.BeginInvoke(Place, DispatcherPriority.Loaded);
        return 0;
    }
    private nint PrimaryMonitor => MonitorFromPoint(new NativeMethods.Point(), 1);
    private MonitorInfo Info()
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(PrimaryMonitor, ref info);
        return info;
    }
    private void Place()
    {
        var info = Info();
        var scale = GetDpiForWindow(_handle) / 96.0;
        if (scale <= 0) scale = 1;
        _lastScale = scale;
        int width = (int)Math.Ceiling(_window.Width * scale);
        int height = (int)Math.Ceiling(_window.Height * scale);
        int x = info.Monitor.Left + (info.Monitor.Right - info.Monitor.Left - width) / 2;
        int y = info.Monitor.Top;
        SetWindowPos(_handle, -1, x, y, width, height, 0x0010);
    }
    private void Tick(object? sender, EventArgs args)
    {
        var foreground = GetForegroundWindow();
        bool fullscreen = false;
        if (foreground != 0 && foreground != _handle && foreground != GetShellWindow() &&
            foreground != GetDesktopWindow() && !IsIconic(foreground) &&
            MonitorFromWindow(foreground, 2) == PrimaryMonitor)
        {
            var monitor = Info().Monitor;
            if (DwmGetWindowAttribute(foreground, 9, out var rect, Marshal.SizeOf<NativeMethods.Rect>()) != 0)
                GetWindowRect(foreground, out rect);
            fullscreen = rect.Left <= monitor.Left && rect.Top <= monitor.Top &&
                         rect.Right >= monitor.Right && rect.Bottom >= monitor.Bottom;
        }
        if (fullscreen != _hidden)
        {
            _hidden = fullscreen;
            if (_hidden) _window.Hide();
            else { _window.Show(); Place(); }
        }
        UpdateReveal(fullscreen);
        if (!_hidden && Math.Abs(GetDpiForWindow(_handle) / 96.0 - _lastScale) > 0.01) Place();
    }
    private void UpdateReveal(bool fullscreen)
    {
        bool reveal = false;
        if (!fullscreen && GetCursorPos(out var cursor))
        {
            var info = Info();
            uint dpi = GetDpiForWindow(_handle);
            double scale = dpi == 0 ? 1 : dpi / 96.0;
            double center = (info.Monitor.Left + info.Monitor.Right) / 2.0;
            // The activation strip is only 8 DIPs tall. Once open, retain
            // the full panel area while the pointer moves to its controls.
            bool trigger = Math.Abs(cursor.X - center) <= 144 * scale &&
                cursor.Y >= info.Monitor.Top && cursor.Y <= info.Monitor.Top + 8 * scale;
            bool inside = _revealed && Math.Abs(cursor.X - center) <= 200 * scale &&
                cursor.Y >= info.Monitor.Top && cursor.Y <= info.Monitor.Top + 176 * scale;
            long now = Environment.TickCount64;
            bool keepOpen = _keepOpen();
            if (trigger || inside || keepOpen) _lastInside = now;
            reveal = trigger || inside || keepOpen || (_revealed && now - _lastInside < 450);
        }
        if (reveal == _revealed) return;
        _revealed = reveal;
        _reveal(reveal);
    }
    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= Tick;
        _source.RemoveHook(Hook);
    }
}

