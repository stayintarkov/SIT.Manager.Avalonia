using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            localServer.Ping = await _serverService.GetPingAsync(localServer);

            List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(localServer);

            AkiCharacter testCharacter = new AkiCharacter(localServer, "nnn", "nnn");

            string ProfileID;
            try
            {
                 ProfileID = await _serverService.RegisterCharacterAsync(testCharacter);
            }
            catch (UsernameTakenException)
            {
                ProfileID = await _serverService.LoginAsync(testCharacter);
            }

            Debugger.Break();
            Debug.WriteLine($"{localServer.Name}'s ping is {localServer.Ping}ms");
        });
    }
}
