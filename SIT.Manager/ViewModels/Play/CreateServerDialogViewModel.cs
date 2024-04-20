using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateServerDialogViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateServer))]
    private string _serverAddress;

    public bool CanCreateServer => !string.IsNullOrEmpty(ServerAddress);

    public CreateServerDialogViewModel(string currentServerAddress)
    {
        ServerAddress = currentServerAddress;
    }
}
