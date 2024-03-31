using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class InstallPage : UserControl
{
    public InstallPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<InstallPageViewModel>();
    }
}
