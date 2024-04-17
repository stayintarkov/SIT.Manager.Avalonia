using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class PlayPage : ActivatableUserControl
{
    public PlayPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<PlayPageViewModel>();
    }
}
