using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation;

public partial class ConfigureServerView : ReactiveUserControl<ConfigureServerViewModel>
{
    public ConfigureServerView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<ConfigureServerViewModel>();
        this.WhenActivated(disposables => { /* Handle view activation etc. */ });
    }
}
