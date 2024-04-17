using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Services;
using SIT.Manager.Views.Play;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSelectionViewModel : ObservableRecipient, IRecipient<DeleteServerMessage>
{
    private readonly IAkiServerRequestingService _serverService;

    public ObservableCollection<ServerSummaryViewModel> ServerList { get; } = [];

    public IAsyncRelayCommand CreateServerCommand { get; }

    public ServerSelectionViewModel(IAkiServerRequestingService serverService)
    {
        _serverService = serverService;

        CreateServerCommand = new AsyncRelayCommand(CreateServer);

        // TODO remove this at some point
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
    }

    private async Task CreateServer()
    {
        CreateServerDialogView dialog = new();
        (ContentDialogResult result, string serverUriString) = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(serverUriString))
        {
            // TODO store these somewhere permanent and then retrieve them on loading this view model.
            ServerList.Add(new ServerSummaryViewModel(serverUriString, App.Current.Services.GetService<ILogger<ServerSummaryViewModel>>(), App.Current.Services.GetService<IAkiServerRequestingService>()));
        }
    }

    public async void Receive(DeleteServerMessage message)
    {
        ContentDialog contentDialog = new()
        {
            Title = "Delete Server",
            Content = "Are you sure you want to delete this server?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No"
        };
        ContentDialogResult result = await contentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ServerSummaryViewModel? server = ServerList.FirstOrDefault(x => x.Address == message.Value);
            if (server != null)
            {
                ServerList.Remove(server);
            }
        }
    }
}
