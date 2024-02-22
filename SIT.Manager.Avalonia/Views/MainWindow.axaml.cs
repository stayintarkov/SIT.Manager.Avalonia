using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Services;
using System;

namespace SIT.Manager.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow() {
        InitializeComponent();
        // This feature doesn't work on linux
        if (OperatingSystem.IsLinux()) {
            CustomTitleBarGrid.IsVisible = false;
        }
    }

    private void Window_Closed(object? sender, EventArgs e) {
        IAkiServerService? akiServerService = App.Current.Services.GetService<IAkiServerService>();
        IManagerConfigService? managerConfig = App.Current.Services.GetService<IManagerConfigService>();
        if (akiServerService?.State == RunningState.Running && (!managerConfig?.Config.CloseAfterLaunch ?? true))
        {
            akiServerService?.Stop();
        }
    }
}
