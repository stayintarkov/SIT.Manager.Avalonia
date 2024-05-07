using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class MainView : ActivatableUserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<MainViewModel>();
    }
}
