using Avalonia.Media;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using System;

namespace SIT.Manager.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        TitleBar.BackgroundColor = new Color(0xFF, 0x00, 0x00, 0x00);
        TitleBar.ForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
        TitleBar.InactiveBackgroundColor = new Color(0xFF, 0x00, 0x00, 0x00);
        TitleBar.InactiveForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
        TitleBar.ButtonBackgroundColor = new Color(0xFF, 0x00, 0x00, 0x00);
        TitleBar.ButtonForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
        TitleBar.ButtonHoverBackgroundColor = new Color(0xFF, 0x11, 0x11, 0x11);
        TitleBar.ButtonHoverForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
        TitleBar.ButtonPressedBackgroundColor = new Color(0xFF, 0x21, 0x21, 0x21);
        TitleBar.ButtonPressedForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
        TitleBar.ButtonInactiveBackgroundColor = new Color(0xFF, 0x00, 0x00, 0x00);
        TitleBar.ButtonInactiveForegroundColor = new Color(0xFF, 0xFF, 0xFF, 0xFF);
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        IAkiServerService? akiServerService = App.Current.Services.GetService<IAkiServerService>();
        IManagerConfigService? managerConfig = App.Current.Services.GetService<IManagerConfigService>();

        bool serverProcessRunning = akiServerService?.State == RunningState.Running || akiServerService?.State == RunningState.Starting;
        if (serverProcessRunning && (!managerConfig?.Config.LauncherSettings.CloseAfterLaunch ?? true))
        {
            akiServerService?.Stop();
        }
    }
}
