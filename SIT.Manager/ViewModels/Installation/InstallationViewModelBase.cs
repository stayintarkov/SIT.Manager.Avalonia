using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Models.Installation;

namespace SIT.Manager.ViewModels.Installation;

//TODO: I would love to replace the current installation system with something more modular, However, I don't have it in me right now
//If I can find the effort to do this after the refactor of everything else I'll get to it, otherwise good luck to anyone
//That wishes to take this task on
public partial class InstallationViewModelBase : ObservableRecipient
{
    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

    protected bool IsServerInstall => CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallServer || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer;
    protected bool IsSitInstall => CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallSit || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit;

    public InstallationViewModelBase()
    {
        try
        {
            CurrentInstallProcessState = Messenger.Send<InstallProcessStateRequestMessage>();
        }
        catch
        {
            CurrentInstallProcessState = new();
        }
    }

    protected void ProgressInstall()
    {
        WeakReferenceMessenger.Default.Send(new InstallProcessStateChangedMessage(CurrentInstallProcessState));
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }

    protected void RegressInstall()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(false));
    }
}
