using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class PatchViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _copyProgressPercentage = 0;

    [ObservableProperty]
    private double _downloadProgressPercentage = 0;

    [ObservableProperty]
    private double _extractionProgressPercentage = 0;

    public PatchViewModel() : base()
    {

    }

    [RelayCommand]
    private void Progress()
    {
        WeakReferenceMessenger.Default.Send(new ProgressInstallMessage(true));
    }
}
