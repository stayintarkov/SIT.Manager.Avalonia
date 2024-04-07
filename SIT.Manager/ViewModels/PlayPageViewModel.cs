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

            //Ping int return
            localServer.Ping = await _serverService.PingAsync(localServer);

            //Ping reference (#AkiServer.Ping will be updated in the method)
            await _serverService.PingByReferenceAsync(localServer);

            Debug.WriteLine($"{localServer.Name}'s ping is {localServer.Ping}ms");
        });
    }
}
