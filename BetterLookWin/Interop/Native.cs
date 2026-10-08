using System;
using System.Runtime.InteropServices;

namespace BetterLookWin.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    public int X;
    public int Y;
}

internal static class Native
{
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;

    public const uint GA_ROOT = 2;
    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;

    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newLong);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>Escala de pantalla (1.0 = 100%, 1.5 = 150%...) del monitor donde está el punto.</summary>
    public static double GetScaleForPoint(NativePoint point)
    {
        try
        {
            var monitor = MonitorFromPoint(point, 2); // MONITOR_DEFAULTTONEAREST
            if (GetDpiForMonitor(monitor, 0, out uint dpiX, out _) == 0 && dpiX > 0)
                return dpiX / 96.0;
        }
        catch
        {
        }
        return 1.0;
    }
}
