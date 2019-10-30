using System;
using System.Windows;
using System.Windows.Interop;
using System.Linq;

namespace BeatDetectorHost
{
    public static class Helpers
    {
        public static Random random = new Random();

        public static void ToTransparentWindow(this Window x)
        {
            //x.SourceInitialized +=
            //    delegate
            //    {
                    // Get this window's handle
                    IntPtr hwnd = new WindowInteropHelper(x).Handle;

                    // Change the extended window style to include WS_EX_TRANSPARENT
                    int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_TRANSPARENT);
                //};
        }
        public static void ToNormalWindow(this Window x)
        {
            //x.SourceInitialized +=
            //    delegate
            //    {
                    // Get this window's handle
                    IntPtr hwnd = new WindowInteropHelper(x).Handle;

                    // Change the extended window style to include WS_EX_TRANSPARENT
                    int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle & ~Win32.WS_EX_TRANSPARENT);
                //};
        }

        public static bool IsOpen(this Window window)
        {
            return Application.Current.Windows.Cast<Window>().Any(x => x == window);
        }
    }
}
