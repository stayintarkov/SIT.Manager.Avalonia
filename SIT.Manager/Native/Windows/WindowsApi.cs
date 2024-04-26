using System;
using System.Runtime.InteropServices;

namespace SIT.Manager.Native.Windows;

public static class WindowsApi
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetParent(nint hWndChild, nint hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
    private static extern uint SetWindowLong32b(nint hWnd, int nIndex, uint value);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
    private static extern nint SetWindowLong64b(nint hWnd, int nIndex, nint value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern nint SetWindowLongPtr64(HandleRef hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

    public static nint SetWindowLongPtr(HandleRef hWnd, int nIndex, nint dwNewLong)
    {
        if (nint.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        else
        {
            return new nint(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }
    }
}
