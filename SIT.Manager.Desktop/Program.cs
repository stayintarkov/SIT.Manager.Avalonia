using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using SIT.Manager.Views;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppBuilder aB = BuildAvaloniaApp();
        try
        {
            aB.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash.log", ex.ToString());
            CrashApp crashApp = new CrashApp();
            crashApp.RunWithMainWindow<CrashWindow>();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
