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
using SIT.Manager.Services;
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
    private string _quickPlayText = "Start Server and Connect";

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
                    CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
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
            AkiServer server = await _serverRequestingService.GetAkiServerAsync(serverAddress);
            AkiCharacter character = new(server, Username, Password);

            try
            {
                await _tarkovClientService.ConnectToServer(character);
            }
            catch (AccountNotFoundException)
            {
                // TODO handle account not found (register?)
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
                Name = _localizationService.TranslateSource("PlayPageViewModelServerAddressTitle"),
                ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelServerAddressDescription"),
                Check = () => { return serverAddress != null; }
            },
            //Install path
            new()
            {
                Name = _localizationService.TranslateSource("PlayPageViewModelInstallPathTitle"),
                ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelInstallPathDescription"),
                Check = () => { return !string.IsNullOrEmpty(_configService.Config.InstallPath); }
            },
            //SIT check
            new()
            {
                Name = _localizationService.TranslateSource("PlayPageViewModelSITInstallationTitle"),
                ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelSITInstallationDescription", SIT_DLL_FILENAME),
                Check = () => { return File.Exists(Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins", SIT_DLL_FILENAME)); }
            },
            //EFT Check
            new()
            {
                Name = _localizationService.TranslateSource("PlayPageViewModelEFTInstallationTitle"),
                ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelEFTInstallationDescription", EFT_EXE_FILENAME),
                Check = () => { return File.Exists(Path.Combine(_configService.Config.InstallPath, EFT_EXE_FILENAME)); }
            },
            //Field Check
            new()
            {
                Name = _localizationService.TranslateSource("PlayPageViewModelInputValidationTitle"),
                ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelInputValidationDescription"),
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
                    Name = _localizationService.TranslateSource("PlayPageViewModelUnhandledAkiInstanceTitle"),
                    ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelUnhandledAkiInstanceDescription"),
                    Check = () => { return !_akiServerService.IsUnhandledInstanceRunning(); }
                },
                //Missing executable
                new ValidationRule()
                {
                    Name = _localizationService.TranslateSource("PlayPageViewModelMissingAKIInstallationTitle"),
                    ErrorMessage = _localizationService.TranslateSource("PlayPageViewModelMissingAKIInstallationDescription"),
                    Check = () => { return File.Exists(_akiServerService.ExecutableFilePath); }
                }
            ]);
        }

        return validationRules;
    }

    private async Task HandleFailedStatus(AkiLoginStatus status)
    {
        switch (status)
        {
            case AkiLoginStatus.IncorrectPassword:
                {
                    await new ContentDialog()
                    {
                        Title = _localizationService.TranslateSource("PlayPageViewModelLoginErrorTitle"),
                        Content = _localizationService.TranslateSource("PlayPageViewModelLoginIncorrectPassword"),
                        CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
                    }.ShowAsync();
                    break;
                }
            case AkiLoginStatus.UsernameTaken:
            default:
                {
                    await new ContentDialog()
                    {
                        Title = _localizationService.TranslateSource("PlayPageViewModelLoginErrorTitle"),
                        Content = _localizationService.TranslateSource("PlayPageViewModelLoginErrorDescription"),
                        CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
                    }.ShowAsync();
                    break;
                }
        }

    }

    private async Task<bool> LaunchServer()
    {
        _akiServerService.Start();

        bool aborted = false;
        RunningState serverState;
        while ((serverState = _akiServerService.State) == RunningState.Starting)
        {
            QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelWaitingForServerTitle");

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
                        Title = _localizationService.TranslateSource("PlayPageViewModelServerErrorTitle"),
                        Content = _localizationService.TranslateSource("PlayPageViewModelServerErrorDescription"),
                        CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
                    }.ShowAsync();
                });
                aborted = true;
                break;
            }

            await Task.Delay(1000);
        }

        QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelQuickPlayText");
        return aborted;
    }

    private async Task<string> RegisterUser(AkiCharacter character)
    {
        _logger.LogDebug("Username {Username} not found....", character.Username);
        ContentDialogResult createAccountResponse = await new ContentDialog()
        {
            Title = _localizationService.TranslateSource("PlayPageViewModelAccountNotFound"),
            Content = _localizationService.TranslateSource("PlayPageViewModelAccountNotFoundDescription"),
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonYes"),
            CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonNo")
        }.ShowAsync();

        if (createAccountResponse == ContentDialogResult.Primary)
        {
            _logger.LogDebug("Registering...");
            (string registerRespStr, AkiLoginStatus status) = await _serverRequestingService.RegisterCharacterAsync(character);
            if (status == AkiLoginStatus.Success)
            {
                _logger.LogDebug("Register successful");
                return registerRespStr;
            }
            else
            {
                _logger.LogDebug("Register failed with {status}", status);
                await HandleFailedStatus(status);
            }
        }

        return string.Empty;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelQuickPlayText");
    }
}
