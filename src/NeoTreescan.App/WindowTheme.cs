using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NeoTreescan.App;

/// Opts the Win32 title bar into dark ("immersive") mode so min/max/close buttons
/// and the caption bar render on a dark background matching the app theme.
/// Works on Windows 10 1809+ (uses attribute 19 on older builds, 20 on newer).
internal static class WindowTheme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Win10 1809 – 2004
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE     = 20; // Win10 20H1+ and Win11

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void ApplyDarkTitleBar(Window window)
    {
        void Apply(IntPtr hwnd)
        {
            int on = 1;
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
            if (hr != 0) // fall back to pre-20H1 attribute code
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));
        }

        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            Apply(helper.Handle);
            return;
        }
        window.SourceInitialized += (_, _) => Apply(new WindowInteropHelper(window).Handle);
    }
}
