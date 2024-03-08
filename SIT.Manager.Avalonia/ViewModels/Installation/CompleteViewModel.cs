using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class CompleteViewModel : ViewModelBase
{
    [RelayCommand]
    private void Progress()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }

    [RelayCommand]
    private void Reset()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }
}
