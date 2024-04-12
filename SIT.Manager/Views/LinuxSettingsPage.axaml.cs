using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class LinuxSettingsPage : UserControl
{
    public LinuxSettingsPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<LinuxSettingsPageViewModel>();
    }
}

