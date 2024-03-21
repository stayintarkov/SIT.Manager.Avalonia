using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Controls;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views;

public partial class ModsPage : ActivatableUserControl
{
    public ModsPage()
    {
        DataContext = App.Current.Services.GetService<ModsPageViewModel>();
        InitializeComponent();
    }
}
