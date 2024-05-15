using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class UpdatePage : ActivatableUserControl
{
    public UpdatePage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<UpdatePageViewModel>();
    }
}
