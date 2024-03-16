using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SIT.Manager.Avalonia.Native;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Controls;

public class EmbeddedProcessWindow(string processPath) : NativeControlHost
{
    private Process? _p;

    public string ProcessPath { get; } = processPath;

    public IntPtr ProcessWindowHandle { get; private set; }

    private void Process_Exited(object? sender, EventArgs e)
    {
        // TODO
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (OperatingSystem.IsWindows())
        {
            // return the ProcessWindowHandle
            return new PlatformHandle(ProcessWindowHandle, "ProcWinHandle");
        }
        else
        {
            return base.CreateNativeControlCore(parent);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Set the parent of the ProcessWindowHandle to be the main window's handle
        IntPtr parentHandle = ((Window) e.Root).TryGetPlatformHandle()?.Handle ?? 0;
        if (parentHandle != IntPtr.Zero)
        {
            while (ProcessWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(200);
            }
            IntPtr result = WindowsApi.SetParent(ProcessWindowHandle, parentHandle);
        }

        // Get the old style of the child window
        long style = WindowsApi.GetWindowLongPtr(ProcessWindowHandle, -16);

        // Modify the style of the ChildWindow - remove the embedded window's frame and other attributes of
        // a stand alone window. Add child flag
        style &= ~0x00010000;
        style &= ~0x00800000;
        style &= ~0x80000000;
        style &= ~0x00400000;
        style &= ~0x00080000;
        style &= ~0x00020000;
        style &= ~0x00040000;
        style |= 0x40000000; // child

        HandleRef handleRef = new(null, ProcessWindowHandle);

        // Set the new style of the schild window
        WindowsApi.SetWindowLongPtr(handleRef, -16, new IntPtr(style));

        base.OnAttachedToVisualTree(e);
    }

    public async Task StartProcess()
    {
        // Start the process
        _p = Process.Start(ProcessPath);

        _p.Exited += Process_Exited;

        // Wait until p.MainWindowHandle is non-zero
        while (true)
        {
            await Task.Delay(200);
            if (_p.MainWindowHandle != IntPtr.Zero)
            {
                break;
            }
        }

        // Set ProcessWindowHandle to the MainWindowHandle of the process
        ProcessWindowHandle = _p.MainWindowHandle;
    }
}
