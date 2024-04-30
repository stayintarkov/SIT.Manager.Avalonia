using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSelectionViewModel : ObservableRecipient
{
    private readonly ILogger<CharacterSelectionViewModel> _logger;
    private readonly IAkiServerRequestingService _serverService;
    private readonly ILocalizationService _localizationService;
    private readonly IManagerConfigService _configService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITarkovClientService _tarkovClientService;

    private AkiServer _connectedServer;

    public ObservableCollection<CharacterSummaryViewModel> SavedCharacterList { get; } = [];
    public ObservableCollection<CharacterSummaryViewModel> CharacterList { get; } = [];

    public IAsyncRelayCommand CreateCharacterCommand { get; }

    public CharacterSelectionViewModel(IServiceProvider serviceProvider,
        ILogger<CharacterSelectionViewModel> logger,
        IAkiServerRequestingService serverService,
        ILocalizationService localizationService,
        IManagerConfigService configService,
        ITarkovClientService tarkovClientService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizationService = localizationService;
        _serverService = serverService;
        _configService = configService;
        _tarkovClientService = tarkovClientService;

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
        if (string.IsNullOrEmpty(_configService.Config.SitVersion) && string.IsNullOrEmpty(_configService.Config.SitTarkovVersion))
        {
            await new ContentDialog()
            {
                Title = _localizationService.TranslateSource("DirectConnectViewModelInstallNotFoundTitle"),
                Content = _localizationService.TranslateSource("DirectConnectViewModelInstallNotFoundMessage"),
                PrimaryButtonText = _localizationService.TranslateSource("DirectConnectViewModelButtonOk"),
            }.ShowAsync();
            return;
        }

        await _tarkovClientService.CreateCharacter(_connectedServer);
        await ReloadCharacterList();
    }

    private async Task ReloadCharacterList()
    {
        CharacterList.Clear();
        SavedCharacterList.Clear();
        try
        {
            //TODO: This is currently listing *all* server characters. We should narrow this to saved only
            List<AkiMiniProfile> miniProfiles = await _serverService.GetMiniProfilesAsync(_connectedServer);
            foreach (AkiMiniProfile profile in miniProfiles)
            {
                CharacterSummaryViewModel characterSummaryViewModel = ActivatorUtilities.CreateInstance<CharacterSummaryViewModel>(_serviceProvider, _connectedServer, profile);
                if (_connectedServer.Characters.Any(x => x.Username == profile.Username) == true)
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
        catch(HttpRequestException ex)
        {
            _logger.LogError(ex, "An error occured while fetching characters");
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        AkiServer? currentServer = _configService.Config.BookmarkedServers.FirstOrDefault(x => x.Address == _connectedServer.Address);
        if (currentServer != null)
        {
            _connectedServer = currentServer;
        }

        await ReloadCharacterList();
    }
}
