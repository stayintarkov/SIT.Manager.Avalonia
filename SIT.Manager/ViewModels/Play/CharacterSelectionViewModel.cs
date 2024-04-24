using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSelectionViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;
    private readonly IManagerConfigService _configService;
    private readonly IServiceProvider _serviceProvider;

    private readonly AkiServer _connectedServer;

    public ObservableCollection<CharacterSummaryViewModel> SavedCharacterList { get; } = [];
    public ObservableCollection<CharacterSummaryViewModel> CharacterList { get; } = [];

    public IAsyncRelayCommand CreateCharacterCommand { get; }

    public CharacterSelectionViewModel(IServiceProvider serviceProvider, ILogger<CharacterSelectionViewModel> logger, IAkiServerRequestingService serverService, IManagerConfigService configService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _serverService = serverService;
        _configService = configService;

        try
        {
            _connectedServer = WeakReferenceMessenger.Default.Send<ConnectedServerRequestMessage>();
        }
        catch
        {
            _connectedServer = new AkiServer(new Uri("http://127.0.0.1:6969"))
            {
                Characters = [],
                Name = "N/A",
                Ping = -1
            };
        }

        CreateCharacterCommand = new AsyncRelayCommand(CreateCharacter);
    }

    [RelayCommand]
    private void Back()
    {
        WeakReferenceMessenger.Default.Send(new ServerDisconnectMessage(_connectedServer));
    }

    private async Task CreateCharacter()
    {
        // TODO
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        AkiServer? currentServer = _configService.Config.BookmarkedServers.FirstOrDefault(x => x.Address == _connectedServer.Address);

        CharacterList.Clear();
        List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(_connectedServer);
        foreach (AkiMiniProfile profile in miniProfiles)
        {
            CharacterSummaryViewModel characterSummaryViewModel = ActivatorUtilities.CreateInstance<CharacterSummaryViewModel>(_serviceProvider, _connectedServer, profile);
            if (currentServer?.Characters.Any(x => x.Username == profile.Username) == true)
            {
                SavedCharacterList.Add(characterSummaryViewModel);
            }
            else
            {
                CharacterList.Add(characterSummaryViewModel);
            }
        }

        _logger.LogDebug("{profileCount} mini profiles retrieved from {name}", miniProfiles.Count, _connectedServer.Name);
    }
}
