using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Views.Play;
using System;

namespace SIT.Manager.ViewModels;

public partial class PlayPageViewModel : ObservableRecipient, IRecipient<ServerConnectMessage>, IRecipient<ConnectedServerRequestMessage>
{
    private AkiServer? _connectedServer;

    [ObservableProperty]
    private UserControl _playControl;

    public PlayPageViewModel()
    {
        PlayControl = new ServerSelectionView();

        /* TODO remove this at some point
        Task.Run(async () =>
        {
            AkiServer localServer = await _serverService.GetAkiServerAsync(new Uri("http://127.0.0.1:6969"));
            Debug.WriteLine($"{localServer.Address.AbsoluteUri} found with name {localServer.Name}");

            localServer.Ping = await _serverService.GetPingAsync(localServer);
            Debug.WriteLine($"{localServer.Name}'s ping is {localServer.Ping}ms");

            List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(localServer);
            Debug.WriteLine($"{miniProfiles.Count} mini profiles retrieved from {localServer.Name}");

            AkiCharacter testCharacter = new AkiCharacter(localServer, "nnn", "nnn");

            string? ProfileID = null;
            if (miniProfiles.Select(x => x.Username == testCharacter.Username).Any())
            {
                Debug.WriteLine($"Username {testCharacter.Username} was already found on server. Attempting to login...");
                (string loginRespStr, AkiLoginStatus status) = await _serverService.LoginAsync(testCharacter);
                if (status == AkiLoginStatus.Success)
                {
                    Debug.WriteLine("Login successful");
                    ProfileID = loginRespStr;
                }
                else
                    Debug.WriteLine($"Failed to login with error {status}");

            }
            else
            {
                Debug.WriteLine($"Username {testCharacter.Username} not found. Registering...");
                (string registerRespStr, AkiLoginStatus status) = await _serverService.RegisterCharacterAsync(testCharacter);
                if (status == AkiLoginStatus.Success)
                {
                    Debug.WriteLine("Register successful");
                    ProfileID = registerRespStr;
                }
                else
                    Debug.WriteLine($"Register failed with {status}");
            }

            if (ProfileID != null)
            {
                testCharacter.ProfileID = ProfileID;
                Debug.WriteLine($"{testCharacter.Username}'s ProfileID is {testCharacter.ProfileID}");
                localServer.Characters.Add(testCharacter);
            }
        });
        */
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
}
