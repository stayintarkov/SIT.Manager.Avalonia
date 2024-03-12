using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class InstallationViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

    protected bool IsServerInstall => CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallServer || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer;
    protected bool IsSitInstall => CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallSit || CurrentInstallProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit;

    public InstallationViewModelBase()
    {
        try
        {
            CurrentInstallProcessState = WeakReferenceMessenger.Default.Send<InstallProcessStateRequestMessage>();
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
