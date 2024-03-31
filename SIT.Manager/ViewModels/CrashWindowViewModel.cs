using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;

namespace SIT.Manager.ViewModels;
public partial class CrashWindowViewModel
{
    [RelayCommand]
    private void ExitLauncher() => Environment.Exit(-1);
    [RelayCommand]
    private void ReportAndClose()
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "https://github.com/stayintarkov/SIT.Manager.Avalonia/issues/new",
                UseShellExecute = true,
            });
            Process.Start(new ProcessStartInfo()
            {
                FileName = AppDomain.CurrentDomain.BaseDirectory + "crash.log",
                UseShellExecute = true,
            });
        }
        catch { } // trying to open a browser + some text editor, if not happens at least exit program.
        Environment.Exit(-1);
    }
}
