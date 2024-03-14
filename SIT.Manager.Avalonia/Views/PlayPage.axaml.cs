using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views;

public partial class PlayPage : ReactiveUserControl<PlayPageViewModel>
{
    public PlayPage()
    {
        InitializeComponent();

        DataContext = App.Current.Services.GetService<PlayPageViewModel>();
        if (DataContext is PlayPageViewModel dataContext)
        {
            AddressBox.LostFocus += (o, e) => { if (!dataContext.ManagerConfig.HideIpAddress) AddressBox.RevealPassword = true; };
        }

        this.WhenActivated(disposables => { /* Handle view activation etc. */ });
    }
}
