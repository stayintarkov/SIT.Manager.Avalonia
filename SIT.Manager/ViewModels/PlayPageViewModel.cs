using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Views.Play;
using System;

namespace SIT.Manager.ViewModels;

public partial class PlayPageViewModel : ObservableRecipient,
                                         IRecipient<ServerConnectMessage>,
                                         IRecipient<ConnectedServerRequestMessage>,
                                         IRecipient<ServerDisconnectMessage>
{
    private AkiServer? _connectedServer;

    [ObservableProperty]
    private UserControl _playControl;

    public PlayPageViewModel()
    {
        PlayControl = new ServerSelectionView();
    }

    public void Receive(ServerConnectMessage message)
    {
        _connectedServer = message.Value;
        PlayControl = new CharacterSelectionView();
    }

    public void Receive(ConnectedServerRequestMessage message)
    {
        if (_connectedServer != null)
        {
            message.Reply(_connectedServer);
        }
        else
        {
            throw new Exception("_connectedServer is null when it shouldn't be");
        }
    }

    public void Receive(ServerDisconnectMessage message)
    {
        _connectedServer = null;
        PlayControl = new ServerSelectionView();
    }
}
