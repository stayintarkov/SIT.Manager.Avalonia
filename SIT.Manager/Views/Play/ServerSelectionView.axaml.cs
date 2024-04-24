using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels.Play;

namespace SIT.Manager.Views.Play;

public partial class ServerSelectionView : ActivatableUserControl
{
    public ServerSelectionView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<ServerSelectionViewModel>();
    }
}
