using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Exceptions;
using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class DirectConnectViewModel : ObservableRecipient
{
    private const string SIT_DLL_FILENAME = "StayInTarkov.dll";
    private const string EFT_EXE_FILENAME = "EscapeFromTarkov.exe";

    private readonly IAkiServerService _akiServerService;
    private readonly IAkiServerRequestingService _serverRequestingService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<DirectConnectViewModel> _logger;
    private readonly IManagerConfigService _configService;
    private readonly ITarkovClientService _tarkovClientService;

    [ObservableProperty]
    private string _lastServer;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private ManagerConfig _managerConfig;

    [ObservableProperty]
    private string _quickPlayText = string.Empty;

    public IAsyncRelayCommand ConnectToServerCommand { get; }
    public IAsyncRelayCommand QuickPlayCommand { get; }

    public DirectConnectViewModel(
        IAkiServerRequestingService serverRequestingService,
        IManagerConfigService configService,
        ITarkovClientService tarkovClientService,
        IAkiServerService akiServerService,
        ILocalizationService localizationService,
        ILogger<DirectConnectViewModel> logger)
    {
        _serverRequestingService = serverRequestingService;
        _configService = configService;
        _managerConfig = _configService.Config;
        _tarkovClientService = tarkovClientService;
        _akiServerService = akiServerService;
        _localizationService = localizationService;
        _managerConfig = configService.Config;
        _logger = logger;

        _lastServer = _configService.Config.LastServer;
        _username = _configService.Config.Username;
        _password = _configService.Config.Password;
        _rememberMe = _configService.Config.RememberLogin;

        ConnectToServerCommand = new AsyncRelayCommand(async () => await ConnectToServer());
        QuickPlayCommand = new AsyncRelayCommand(async () => await ConnectToServer(true));
    }

    private async Task ConnectToServer(bool launchServer = false)
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

        ManagerConfig config = _configService.Config;
        config.Username = Username;
        config.Password = Password;
        config.LastServer = LastServer;
        config.RememberLogin = RememberMe;
        _configService.UpdateConfig(config, true, config.RememberLogin);

        Uri? serverAddress = GetUriFromAddress(LastServer);

        List<ValidationRule> validationRules = GenerateValidationRules(serverAddress, launchServer);
        foreach (ValidationRule rule in validationRules)
        {
            if (rule?.Check != null && !rule.Check())
            {
                await new ContentDialog()
                {
                    Title = rule?.Name,
                    Content = rule?.ErrorMessage,
                    CloseButtonText = _localizationService.TranslateSource("DirectConnectViewModelButtonOk")
                }.ShowAsync();
                return;
            }
        }

        if (launchServer)
        {
            bool aborted = await LaunchServer();
            if (aborted)
            {
                // TODO log aborted :)
                return;
            }
        }

        if (serverAddress != null)
        {
            AkiServer server = await _serverRequestingService.GetAkiServerAsync(serverAddress, false);
            AkiCharacter character = new(server, Username, Password);

            try
            {
                await _tarkovClientService.ConnectToServer(character);
            }
            catch (AccountNotFoundException)
            {
                ContentDialogResult createAccountResponse = await new ContentDialog()
                {
                    Title = _localizationService.TranslateSource("DirectConnectViewModelAccountNotFound"),
                    Content = _localizationService.TranslateSource("DirectConnectViewModelAccountNotFoundDescription"),
                    PrimaryButtonText = _localizationService.TranslateSource("DirectConnectViewModelButtonYes"),
                    CloseButtonText = _localizationService.TranslateSource("DirectConnectViewModelButtonNo")
                }.ShowAsync();
                if (createAccountResponse == ContentDialogResult.Primary)
                {
                    AkiCharacter? newCharacter = await _tarkovClientService.CreateCharacter(server, Username, Password, RememberMe);
                    if (newCharacter != null)
                    {
                        await ConnectToServer(false);
                    }
                }
            }
        }
    }

    private Uri? GetUriFromAddress(string addressString)
    {
        try
        {
            UriBuilder addressBuilder = new(addressString);
            addressBuilder.Port = addressBuilder.Port == 80 ? 6969 : addressBuilder.Port;
            return addressBuilder.Uri;
        }
        catch (UriFormatException)
        {
            return null;
        }
        catch (Exception ex)
        {
            // Something BAAAAD has happened here
            _logger.LogError(ex, "No idea what happened but we didn't manager to get the server uri");
            return null;
        }
    }

    private List<ValidationRule> GenerateValidationRules(Uri? serverAddress, bool launchServer)
    {
        List<ValidationRule> validationRules =
        [
            //Address
            new()
            {
                Name = _localizationService.TranslateSource("DirectConnectViewModelServerAddressTitle"),
                ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelServerAddressDescription"),
                Check = () => { return serverAddress != null; }
            },
            //Install path
            new()
            {
                Name = _localizationService.TranslateSource("DirectConnectViewModelInstallPathTitle"),
                ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelInstallPathDescription"),
                Check = () => { return !string.IsNullOrEmpty(_configService.Config.SitEftInstallPath); }
            },
            //SIT check
            new()
            {
                Name = _localizationService.TranslateSource("DirectConnectViewModelSITInstallationTitle"),
                ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelSITInstallationDescription", SIT_DLL_FILENAME),
                Check = () => { return File.Exists(Path.Combine(_configService.Config.SitEftInstallPath, "BepInEx", "plugins", SIT_DLL_FILENAME)); }
            },
            //EFT Check
            new()
            {
                Name = _localizationService.TranslateSource("DirectConnectViewModelEFTInstallationTitle"),
                ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelEFTInstallationDescription", EFT_EXE_FILENAME),
                Check = () => { return File.Exists(Path.Combine(_configService.Config.SitEftInstallPath, EFT_EXE_FILENAME)); }
            },
            //Field Check
            new()
            {
                Name = _localizationService.TranslateSource("DirectConnectViewModelInputValidationTitle"),
                ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelInputValidationDescription"),
                Check = () => { return LastServer.Length > 0 && Username.Length > 0 && Password.Length > 0; }
            }
        ];

        if (launchServer)
        {
            validationRules.AddRange(
            [
                //Unhandled Instance
                new ValidationRule()
                {
                    Name = _localizationService.TranslateSource("DirectConnectViewModelUnhandledAkiInstanceTitle"),
                    ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelUnhandledAkiInstanceDescription"),
                    Check = () => { return !_akiServerService.IsUnhandledInstanceRunning(); }
                },
                //Missing executable
                new ValidationRule()
                {
                    Name = _localizationService.TranslateSource("DirectConnectViewModelMissingAKIInstallationTitle"),
                    ErrorMessage = _localizationService.TranslateSource("DirectConnectViewModelMissingAKIInstallationDescription"),
                    Check = () => { return File.Exists(_akiServerService.ExecutableFilePath); }
                }
            ]);
        }

        return validationRules;
    }

    private async Task<bool> LaunchServer()
    {
        _akiServerService.Start();

        bool aborted = false;
        RunningState serverState;
        while ((serverState = _akiServerService.State) == RunningState.Starting)
        {
            QuickPlayText = _localizationService.TranslateSource("DirectConnectViewModelWaitingForServerTitle");

            if (serverState == RunningState.Running)
            {
                // We're done the server is running now
                break;
            }
            else if (serverState != RunningState.Starting)
            {
                // We have a state that is not right so need to alert the user and abort
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    new ContentDialog()
                    {
                        Title = _localizationService.TranslateSource("DirectConnectViewModelServerErrorTitle"),
                        Content = _localizationService.TranslateSource("DirectConnectViewModelServerErrorDescription"),
                        CloseButtonText = _localizationService.TranslateSource("DirectConnectViewModelButtonOk")
                    }.ShowAsync();
                });
                aborted = true;
                break;
            }

            await Task.Delay(1000);
        }

        QuickPlayText = _localizationService.TranslateSource("DirectConnectViewModelQuickPlayText");
        return aborted;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        QuickPlayText = _localizationService.TranslateSource("DirectConnectViewModelQuickPlayText");
    }
}
