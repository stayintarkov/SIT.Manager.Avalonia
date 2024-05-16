using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Settings;

namespace SIT.Manager.Views.Settings;
public partial class LinuxView : ActivatableUserControl
{
    public LinuxView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<LinuxViewModel>();
    }
}
