using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class InstallationViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private InstallProcessState _currentInstallProcessState;

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
