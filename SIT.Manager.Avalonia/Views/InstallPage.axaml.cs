using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views;

public partial class InstallPage : UserControl
{
    public InstallPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<InstallPageViewModel>();
    }
}
