using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSelectionViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;

    private readonly AkiServer _connectedServer;

    public ObservableCollection<AkiMiniProfile> ServerProfiles { get; } = [];

    public CharacterSelectionViewModel(ILogger<CharacterSelectionViewModel> logger, IAkiServerRequestingService serverService)
    {
        _logger = logger;
        _serverService = serverService;

        try
        {
            _connectedServer = WeakReferenceMessenger.Default.Send<ConnectedServerRequestMessage>();
        }
        catch (Exception ex)
        {
            _connectedServer = new AkiServer(new Uri("http://127.0.0.1:6969"))
            {
                Characters = [],
                Name = "N/A",
                Ping = -1
            };
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        ServerProfiles.Clear();
        List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(_connectedServer);
        ServerProfiles.AddRange(miniProfiles);
        _logger.LogDebug("{profileCount} mini profiles retrieved from {name}", miniProfiles.Count, _connectedServer.Name);
    }
}
