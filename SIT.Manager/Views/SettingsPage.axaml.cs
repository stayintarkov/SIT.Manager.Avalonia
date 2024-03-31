using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<SettingsPageViewModel>();
    }
}
