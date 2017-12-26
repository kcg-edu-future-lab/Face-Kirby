using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FaceKirby
{
    public static class WindowHelper
    {
        public static void SetInactive(this Window window)
        {
            // Loaded イベントでも動作します。
            window.SourceInitialized += (o, e) => NativeWindowHelper.SetWindowAttribute(window, NativeWindowHelper.GWL_EXSTYLE, NativeWindowHelper.WS_EX_NOACTIVATE);
        }
    }

    static class NativeWindowHelper
    {
        // GetWindowLong function
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms633584(v=vs.85).aspx
        // SetWindowLong function
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms633591(v=vs.85).aspx
        // Extended Window Styles
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx

        public const int GWL_EXSTYLE = -20;

        public const int WS_EX_NOACTIVATE = 0x08000000;

        public static void SetWindowAttribute(Window window, int index, int value)
        {
            var helper = new WindowInteropHelper(window);

            var state = NativeMethods.GetWindowLong(helper.Handle, index);
            NativeMethods.SetWindowLong(helper.Handle, index, state | value);
        }

        static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        }
    }
}
