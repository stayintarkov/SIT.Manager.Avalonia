using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SIT.Manager.Exceptions;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models;
using SIT.Manager.Models.Aki;
using SIT.Manager.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class PlayPageViewModel : ObservableObject
{
    private readonly IAkiServerRequestingService _serverService;
    public PlayPageViewModel(IAkiServerRequestingService serverService)
    {
        _serverService = serverService;

        Task.Run(async () =>
        {
            AkiServer localServer = await _serverService.GetAkiServerAsync(new Uri("http://127.0.0.1:6969"));
            Debug.WriteLine($"{localServer.Address.AbsoluteUri} found with name {localServer.Name}");

            localServer.Ping = await _serverService.GetPingAsync(localServer);
            Debug.WriteLine($"{localServer.Name}'s ping is {localServer.Ping}ms");

            List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(localServer);
            Debug.WriteLine($"{miniProfiles.Count} mini profiles retrieved from {localServer.Name}");

            AkiCharacter testCharacter = new AkiCharacter(localServer, "nnn", "nnn");

            string ProfileID;
            if(miniProfiles.Select(x => x.Username == testCharacter.Username).Any())
            {
                Debug.WriteLine($"Username {testCharacter.Username} was already found on server. Attempting to login...");
                ProfileID = await _serverService.LoginAsync(testCharacter);
            }
            else
            {
                Debug.WriteLine($"Username {testCharacter.Username} not found. Registering...");
                ProfileID = await _serverService.RegisterCharacterAsync(testCharacter);
            }

            testCharacter.ProfileID = ProfileID;
            Debug.WriteLine($"{testCharacter.Username}'s ProfileID is {testCharacter.ProfileID}");

            localServer.Characters.Add(testCharacter);
        });
    }
}
