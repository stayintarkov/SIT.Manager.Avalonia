using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Models;
using SIT.Manager.Models.Installation;
using SIT.Manager.Models.Messages;
using SIT.Manager.Views;

namespace SIT.Manager.ViewModels.Installation;

public partial class CompleteViewModel : InstallationViewModelBase
{
    public CompleteViewModel() : base()
    {
        Messenger.Send(new InstallationRunningMessage(false));
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

        Messenger.Send(new PageNavigationMessage(pageNavigation));
    }
}
