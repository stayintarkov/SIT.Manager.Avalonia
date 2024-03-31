using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SIT.Manager.Native;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SIT.Manager.Controls;

public class EmbeddedProcessWindow(Process p) : NativeControlHost
{
    private readonly Process _p = p;

    public int ExitCode => _p.ExitCode;

    public IntPtr ProcessWindowHandle { get; private set; }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (OperatingSystem.IsWindows())
        {
            _p.Dispose();
        }
        else
        {
            base.DestroyNativeControlCore(control);
        }
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
        if (OperatingSystem.IsWindows())
        {
            // modify the style of the child window

            // get the old style of the child window
            long style = WindowsApi.GetWindowLongPtr(ProcessWindowHandle, -16);

            // modify the style of the ChildWindow - remove the embedded window's frame and other attributes of
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

            // set the new style of the schild window
            WindowsApi.SetWindowLongPtr(handleRef, -16, (IntPtr) style);

            // set the parent of the ProcessWindowHandle to be the main window's handle
            IntPtr parentHandle = ((Window) e.Root).TryGetPlatformHandle()?.Handle ?? 0;
            if (parentHandle != IntPtr.Zero)
            {
                WindowsApi.SetParent(ProcessWindowHandle, parentHandle);
            }
        }
        base.OnAttachedToVisualTree(e);
    }

    public async Task WaitForExit()
    {
        await _p.WaitForExitAsync();
    }

    public async Task StartProcess()
    {
        // Start the process
        _p.Start();

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Wait until p.MainWindowHandle is non-zero
                while (_p.MainWindowHandle == IntPtr.Zero)
                {
                    // Discard cached information about the process because MainWindowHandle might be cached.
                    _p.Refresh();
                    await Task.Delay(250);
                }

                // Set ProcessWindowHandle to the MainWindowHandle of the process
                ProcessWindowHandle = _p.MainWindowHandle;
            }
        }
        catch
        {
            // The process has probably exited, so accessing MainWindowHandle threw an exception
            throw;
        }
    }
}
