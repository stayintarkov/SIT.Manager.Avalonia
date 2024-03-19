using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Installation;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Views;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class CompleteViewModel : InstallationViewModelBase
{
    public CompleteViewModel() : base()
    {
        WeakReferenceMessenger.Default.Send(new InstallationRunningMessage(false));
    }

    [RelayCommand]
    private void Reset()
    {
        PageNavigation pageNavigation;
        if (CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallServer || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer)
        {
            pageNavigation = new(typeof(ServerPage), false);
        }
        else
        {
            pageNavigation = new(typeof(PlayPage), false);
        }

        CurrentInstallProcessState = new();
        ProgressInstall();

        WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
    }
}
