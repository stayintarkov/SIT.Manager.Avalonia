using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels.Settings;

namespace SIT.Manager.Views.Settings;

public partial class EftView : ActivatableUserControl
{
    public EftView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<EftViewModel>();
    }
}
