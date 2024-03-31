using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class ModsPage : ActivatableUserControl
{
    public ModsPage()
    {
        DataContext = App.Current.Services.GetService<ModsPageViewModel>();
        InitializeComponent();
    }
}
