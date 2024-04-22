using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Timers;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateServerDialogViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateServer))]
    private string _serverAddress;
    private readonly Timer _validationTimer = new Timer() { Interval = 500, AutoReset = false, Enabled = false };
    public Uri? ServerUri;

    public bool CanCreateServer => ServerUri != null && !ServerUri.IsDefaultPort;

    public CreateServerDialogViewModel(string currentServerAddress)
    {
        ServerAddress = currentServerAddress ?? string.Empty;
        _validationTimer.Elapsed += (o, e) => ValidateAddress(ServerAddress);
    }

    private void ValidateAddress(string address)
    {
        try
        {
            ServerUri = new Uri(address);
        }
        catch (UriFormatException)
        {
            ServerUri = null;
        }
        finally
        {
            OnPropertyChanged(nameof(CanCreateServer));
        }
    }

    partial void OnServerAddressChanging(string value)
    {
        //Why microsoft
        _validationTimer.Stop();
        _validationTimer.Start();
        if (ServerAddress != null && value.Length < ServerAddress.Length)
            ValidateAddress(value);
    }
}
