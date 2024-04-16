using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateServerDialogViewModel : ObservableObject
{
    private const string DEFAULT_SERVER_ADDRESS = "http://127.0.0.1:6969";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateServer))]
    private string _serverAddress = DEFAULT_SERVER_ADDRESS;

    public bool CanCreateServer => !string.IsNullOrEmpty(ServerAddress);

    public CreateServerDialogViewModel() { }
}
