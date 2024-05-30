using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Aki;
using SIT.Manager.Services.Caching;
using SIT.Manager.Views.Play;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSummaryViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IManagerConfigService _configService;
    private readonly ILocalizationService _localizationService;
    private readonly ITarkovClientService _tarkovClientService;
    private readonly ICachingService _cachingService;
    private readonly IAkiServerRequestingService _akiServerRequestingService;

    private readonly AkiServer _connectedServer;
    private readonly AkiCharacter? character;

    [ObservableProperty]
    private AkiMiniProfile _profile;

    [ObservableProperty]
    private Bitmap? _sideImage;

    [ObservableProperty]
    private double _levelProgressPercentage = 0;

    [ObservableProperty]
    private int _nextLevel = 0;

    [ObservableProperty]
    private bool _requireLogin = true;

    [ObservableProperty]
    public bool _canLaunch = true;

    public IAsyncRelayCommand PlayCommand { get; }

    public IAsyncRelayCommand LogoutCommand { get; }

    public CharacterSummaryViewModel(AkiServer server,
        AkiMiniProfile profile,
        ILocalizationService localizationService,
        ILogger<CharacterSummaryViewModel> logger,
        IManagerConfigService configService,
        ITarkovClientService tarkovClientService,
        ICachingService cachingService,
        IAkiServerRequestingService akiServerRequestingService)
    {
        _logger = logger;
        _configService = configService;
        _localizationService = localizationService;
        _tarkovClientService = tarkovClientService;
        _cachingService = cachingService;
        _akiServerRequestingService = akiServerRequestingService;

        _connectedServer = server;
        Profile = profile;

        double requiredExperience = Profile.NextExperience - Profile.PreviousExperience;
        double currentExperienceProgress = profile.CurrentExperience - Profile.PreviousExperience;
        LevelProgressPercentage = currentExperienceProgress / requiredExperience * 100;

        NextLevel = Profile.CurrentLevel + 1;
        if (Profile.CurrentLevel == Profile.MaxLevel)
        {
            NextLevel = Profile.MaxLevel;
        }

        character = _connectedServer.Characters.FirstOrDefault(x => x.Username == Profile.Username);
        if (character != null)
        {
            int serverIndex = _configService.Config.BookmarkedServers.FindIndex(x => x.Address == _connectedServer.Address);
            if (serverIndex != -1)
            {
                character = _configService.Config.BookmarkedServers[serverIndex].Characters.FirstOrDefault(x => x?.Username == character.Username, null);
                RequireLogin = string.IsNullOrEmpty(character?.Password);
            }
        }

        Task.Run(SetSideImage);

        PlayCommand = new AsyncRelayCommand(Play);

        // In an ideal world we would use OnActivated and OnDeactivated - which are implemented from IActivatableViewModel in the Avalonia.ReactiveUI package.
        // However, this would also require changes in the CharacterSummaryView class - for not this implementation, while crude, does suffice.
        // It may be worth implementing Avalonia.ReactiveUI.IActivatableViewModel at a later date for all pages as part of a larger refactor.
        _tarkovClientService.RunningStateChanged += TarkovClient_RunningStateChanged;
        LogoutCommand = new AsyncRelayCommand(Logout);
    }

    private async Task SetSideImage()
    {
        if (!string.IsNullOrEmpty(Profile.Side) && !Profile.Side.Equals("unknown", StringComparison.InvariantCultureIgnoreCase))
        {
            string cacheKey = $"side_{Profile.Side} icon";
            //TODO: Change this from bitmap to memorystream and load it into bitmap. This will allow us to save to disk
            CacheValue<Bitmap> cacheVal = await _cachingService.InMemory.GetOrComputeAsync<Bitmap>(cacheKey, async (key) =>
            {
                return new(await _akiServerRequestingService.GetAkiServerImage(_connectedServer, $"launcher/side_{Profile.Side.ToLower()}.png"));
            });
            if (cacheVal.HasValue && cacheVal.Value != null)
                SideImage = cacheVal.Value;
        }
        else
        {
            //TODO: Set a default. Not done because idk what to set it to
        }
    }

    private async Task Play()
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

        AkiCharacter? character = _connectedServer.Characters.FirstOrDefault(x => x.Username == Profile.Username);

        // Set this to false rather than true - this was causing duplicate saved profiles
        // If we were already logged on the code to see if EFT was launched AND remember password would pass and save a duplicate each time
        bool rememberLogin = false;

        if (character == null)
        {
            LoginDialogView dialog = new(Profile.Username);
            (ContentDialogResult result, string password, rememberLogin) = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
            character = new(Profile.Username, password);
        }

        try
        {
            bool success = await _tarkovClientService.ConnectToServer(_connectedServer, character);
            if (success && rememberLogin)
            {
                _connectedServer.Characters.Add(character);
                _configService.UpdateConfig(_configService.Config);
                RequireLogin = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to connect to Tarkov server");
            ContentDialog errorDialog = new()
            {
                Title = _localizationService.TranslateSource("CharacterSummaryViewModelPlayErrorDialogTitle"),
                Content = _localizationService.TranslateSource("CharacterSummaryViewModelPlayErrorDialogContent"),
                PrimaryButtonText = _localizationService.TranslateSource("CharacterSummaryViewModelPlayErrorDialogPrimaryButtonText"),
            };
            await errorDialog.ShowAsync();
        }
    }

    private void TarkovClient_RunningStateChanged(object? sender, RunningState runningState)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            switch (runningState)
            {
                case RunningState.Starting:
                case RunningState.Running:
                    CanLaunch = false;

                    break;
                case RunningState.NotRunning:
                case RunningState.StoppedUnexpectedly:
                    CanLaunch = true;

                    break;
            }
        });
    }
    
    private async Task Logout()
    {
        if (character != null)
        {
            _connectedServer.Characters.Remove(character);
            _configService.UpdateConfig(_configService.Config);
            RequireLogin = true;
        }
    }
}
