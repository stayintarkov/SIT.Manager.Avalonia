using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation;

public partial class InstallView : ReactiveUserControl<InstallViewModel>
{
    public InstallView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<InstallViewModel>();
        this.WhenActivated(disposables => { /* Handle view activation etc. */ });
    }
}
