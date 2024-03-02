using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Classes;
using SIT.Manager.Avalonia.Exceptions;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels
{
    public partial class PlayPageViewModel : ViewModelBase
    {
        //TODO: Merge these constants into one play. Pretty sure we delcare at least one of these somewhere else already
        private const string SIT_DLL_FILENAME = "StayInTarkov.dll";
        private const string EFT_EXE_FILENAME = "EscapeFromTarkov.exe";
        private readonly IManagerConfigService _configService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _lastServer;

        [ObservableProperty]
        private string _username;

        [ObservableProperty]
        private string _password;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string _quickPlayText = "Start Server and Connect";

        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly ITarkovClientService _tarkovClientService;
        private readonly IAkiServerService _akiServerService;
        private readonly ILocalizationService _localizationService;

        public IAsyncRelayCommand ConnectToServerCommand { get; }
        public IAsyncRelayCommand QuickPlayCommand { get; }

        public PlayPageViewModel(
            IManagerConfigService configService,
            HttpClient httpClient,
            HttpClientHandler httpClientHandler,
            ITarkovClientService tarkovClientService,
            IAkiServerService akiServerService,
            ILocalizationService localizationService,
            IServiceProvider serviceProvider) 
        {
            _configService = configService;
            //TODO: Check that this is the best way to implement DI for the TarkovRequesting. Prettysure service provider would be better
            _httpClient = httpClient;
            _httpClientHandler = httpClientHandler;
            _tarkovClientService = tarkovClientService;
            _akiServerService = akiServerService;
            _serviceProvider = serviceProvider;
            _localizationService = localizationService;

            QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelQuickPlayText");
            _configService.ConfigChanged += (o, e) =>
            {
                QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelQuickPlayText");
            };

            _lastServer = _configService.Config.LastServer;
            _username = _configService.Config.Username;
            _password = _configService.Config.Password;
            _rememberMe = _configService.Config.RememberLogin;

            ConnectToServerCommand = new AsyncRelayCommand(async () => await ConnectToServer());
            QuickPlayCommand = new AsyncRelayCommand(async () => await ConnectToServer(true));
        }

        private string CreateLaunchArugments(TarkovLaunchConfig launchConfig, string token)
        {
            string jsonConfig = JsonSerializer.Serialize(launchConfig);

            // The json needs single quotes on Linux for some reason even though not valid json
            // but this seems to work fine on Windows too so might as well do it on both ¯\_(ツ)_/¯
            jsonConfig = jsonConfig.Replace('\"', '\'');

            Dictionary<string, string> argumentList = new()
            {
                { "-token", token },
                { "-config", jsonConfig }
            };

            string launchArguments = string.Join(' ', argumentList.Select(argument => $"{argument.Key}={argument.Value}"));
            if (OperatingSystem.IsLinux()) {
                // We need to make sure that the json is contained in quotes on Linux otherwise you won't be able to connect to the server.
                launchArguments = string.Join(' ', argumentList.Select(argument => $"{argument.Key}=\"{argument.Value}\""));
            }
            return launchArguments;
        }

        //TODO: Refactor this so avoid the repeat after registering. This also violates the one purpose rule anyway
        private async Task<string> LoginToServerAsync(Uri address)
        {
            TarkovRequesting requesting = ActivatorUtilities.CreateInstance<TarkovRequesting>(_serviceProvider, address);
            TarkovLoginInfo loginInfo = new() {
                Username = Username,
                Password = Password,
                BackendUrl = address.AbsoluteUri.Trim(['/', '\\'])
            };

            try {
                string SessionID = await requesting.LoginAsync(loginInfo);
                return SessionID;
            }
            catch (AccountNotFoundException) {
                AkiServerConnectionResponse serverResponse = await requesting.QueryServer();

                TarkovEdition[] editions = new TarkovEdition[serverResponse.Editions.Length];
                for (int i = 0; i < editions.Length; i++) {
                    string editionStr = serverResponse.Editions[i];
                    string descriptionStr = serverResponse.Descriptions[editionStr];
                    editions[i] = new TarkovEdition(editionStr, descriptionStr);
                }

                ContentDialogResult createAccountResponse = await new ContentDialog() {
                    Title = _localizationService.TranslateSource("PlayPageViewModelAccountNotFound"),
                    Content = _localizationService.TranslateSource("PlayPageViewModelAccountNotFoundDescription"),
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonYes"),
                    CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonNo")
                }.ShowAsync();

                if (createAccountResponse == ContentDialogResult.Primary) {
                    SelectEditionDialog selectEditionDialog = new SelectEditionDialog(editions);
                    loginInfo.Edition = (await selectEditionDialog.ShowAsync()).Edition;

                    //Register new account
                    await requesting.RegisterAccountAsync(loginInfo);

                    //Attempt to login after registering
                    return await requesting.LoginAsync(loginInfo);
                }
                else
                    return string.Empty;
            }
            catch (IncorrectServerPasswordException) {
                Debug.WriteLine("DEBUG: Incorrect password");
                //TODO: Utils.ShowInfoBar("Connect", $"Invalid password!", InfoBarSeverity.Error);
            }
            catch (Exception ex) {
                await new ContentDialog() {
                    Title = _localizationService.TranslateSource("PlayPageViewModelLoginErrorTitle"),
                    Content = _localizationService.TranslateSource("PlayPageViewModelLoginErrorDescription", ex.Message),
                    CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
                }.ShowAsync();
            }
            return string.Empty;
        }

        private static Uri? GetUriFromAddress(string addressString)
        {
            try {
                UriBuilder addressBuilder = new(addressString);
                addressBuilder.Port = addressBuilder.Port == 80 ? 6969 : addressBuilder.Port;
                return addressBuilder.Uri;
            }
            catch (UriFormatException) {
                return null;
            }
            catch (Exception) {
                //Something BAAAAD has happened here
                //TODO: Loggy & content dialog
                return null;
            }
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
            //TODO: Change this to pass the server address as a param and move this outside the connect method
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
                    Check = () =>{ return File.Exists(Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins", SIT_DLL_FILENAME)); }
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

            if (launchServer) {
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

            foreach (ValidationRule rule in validationRules) {
                if (rule?.Check != null && !rule.Check()) {
                    await new ContentDialog() {
                        Title = rule?.Name,
                        Content = rule?.ErrorMessage,
                        CloseButtonText = _localizationService.TranslateSource("PlayPageViewModelButtonOk")
                    }.ShowAsync();
                    return;
                }
            }

            if (launchServer) {
                _akiServerService.Start();

                bool aborted = false;
                RunningState serverState;
                while ((serverState = _akiServerService.State) == RunningState.Starting) {
                    QuickPlayText = _localizationService.TranslateSource("PlayPageViewModelWaitingForServerTitle");

                    if (serverState == RunningState.Running) {
                        // We're done the server is running now
                        break;
                    }
                    else if (serverState != RunningState.Starting) {
                        // We have a state that is not right so need to alert the user and abort
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            new ContentDialog() {
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
                if (aborted) {
                    return;
                }
            }

            //Connect to server
            string token = await LoginToServerAsync(serverAddress ?? throw new ArgumentNullException(nameof(serverAddress), "Server address is null"));
            if (string.IsNullOrEmpty(token))
                return;

            // TODO: Temporary fix for SIT 1.9 expecting no ending slash in backendUrl URI. To be removed when SIT 1.9 is no longer used as this was addressed in SIT 1.10+.
            string backendUrlFixed = serverAddress.AbsoluteUri.Substring(0, serverAddress.AbsoluteUri.Length - 1);

            // Launch game
            string launchArguments = CreateLaunchArugments(new TarkovLaunchConfig { BackendUrl = backendUrlFixed }, token);
            _tarkovClientService.Start(launchArguments);

            if (_configService.Config.CloseAfterLaunch) {
                IApplicationLifetime? lifetime = App.Current.ApplicationLifetime;
                if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime) {
                    desktopLifetime.Shutdown();
                }
                else {
                    Environment.Exit(0);
                }
            }
        }
    }
}