using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Models.Installation;
using System;

namespace SIT.Manager.ViewModels.Installation;

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
