using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Models.Aki;
using System;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateServerDialogViewModel : ObservableObject
{
    private const string DEFAULT_SERVER_ADDRESS = "http://127.0.0.1:6969";

    public AkiServer? ServerData { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateServer))]
    private string _serverName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateServer))]
    private string _serverAddress = DEFAULT_SERVER_ADDRESS;

    public bool CanCreateServer => !string.IsNullOrEmpty(ServerName) && !string.IsNullOrEmpty(ServerAddress);

    public CreateServerDialogViewModel() { }

    [RelayCommand]
    private void CreateServerData()
    {
        ServerData = new AkiServer(new Uri(ServerAddress))
        {
            Name = ServerName
        };
    }
}
