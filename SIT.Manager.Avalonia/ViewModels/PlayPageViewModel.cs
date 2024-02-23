using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SIT.Manager.Avalonia.Classes;
using SIT.Manager.Avalonia.Classes.Exceptions;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly ITarkovClientService _tarkovClientService;
        private readonly IAkiServerService _akiServerService;

        public IAsyncRelayCommand ConnectToServerCommand { get; }
        public IAsyncRelayCommand QuickPlayCommand { get; }

        public PlayPageViewModel(
            IManagerConfigService configService,
            HttpClient httpClient,
            HttpClientHandler httpClientHandler,
            ITarkovClientService tarkovClientService,
            IAkiServerService akiServerService,
            IServiceProvider serviceProvider)
        {
            _configService = configService;
            //TODO: Check that this is the best way to implement DI for the TarkovRequesting. Prettysure service provider would be better
            _httpClient = httpClient;
            _httpClientHandler = httpClientHandler;
            _tarkovClientService = tarkovClientService;
            _akiServerService = akiServerService;
            _serviceProvider = serviceProvider;

            _lastServer = _configService.Config.LastServer;
            _username = _configService.Config.Username;
            _password = _configService.Config.Password;
            _rememberMe = _configService.Config.RememberLogin;

            ConnectToServerCommand = new AsyncRelayCommand(ConnectToServer);
            QuickPlayCommand = new AsyncRelayCommand(QuickPlay);
        }


        //TODO: Refactor this so avoid the repeat after registering. This also violates the one purpose rule anyway
        private async Task<string> LoginToServerAsync(Uri address)
        {
            TarkovRequesting requesting = ActivatorUtilities.CreateInstance<TarkovRequesting>(_serviceProvider, address);
            TarkovLoginInfo loginInfo = new()
            {
                Username = Username,
                Password = Password,
                BackendUrl = address.AbsoluteUri.Trim(['/', '\\'])
            };

            try
            {
                string SessionID = await requesting.LoginAsync(loginInfo);
                return SessionID;
            }
            catch (AccountNotFoundException)
            {
                AkiServerConnectionResponse serverResponse = await requesting.QueryServer();

                TarkovEdition[] editions = new TarkovEdition[serverResponse.Editions.Length];
                for (int i = 0; i < editions.Length; i++)
                {
                    string editionStr = serverResponse.Editions[i];
                    string descriptionStr = serverResponse.Descriptions[editionStr];
                    editions[i] = new TarkovEdition(editionStr, descriptionStr);
                }

                ContentDialogResult createAccountResponse = await new ContentDialog()
                {
                    Title = "Account Not Found",
                    Content = "Your account has not been found, would you like to register a new account with these credentials?",
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No"
                }.ShowAsync();

                if (createAccountResponse == ContentDialogResult.Primary)
                {
                    //TODO: SelectEditionDialog
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
            catch(IncorrectServerPasswordException)
            {
                Debug.WriteLine("DEBUG: Incorrect password");
                //TODO: Utils.ShowInfoBar("Connect", $"Invalid password!", InfoBarSeverity.Error);
            }
            catch (Exception ex)
            {
                await new ContentDialog()
                {
                    Title = "Login Error",
                    Content = $"Unable to communicate with the server\n{ex.Message}",
                    CloseButtonText = "Ok"
                }.ShowAsync();
            }
            return string.Empty;
        }

        private static Uri? GetUriFromAddress(string addressString)
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
            catch(Exception ex)
            {
                //Something BAAAAD has happened here
                //TODO: Loggy & content dialog
                return null;
            }
        }

        private async Task ConnectToServer()
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
                    Name = "Server Address",
                    ErrorMessage = "Please fix your server address and try again",
                    Check = () => { return serverAddress != null; }
                },
                //Install path
                new()
                {
                    Name = "Install Path",
                    ErrorMessage = "'Install Path' is not configured. Go to settings and configure the installtion path.",
                    Check = () => { return !string.IsNullOrEmpty(_configService.Config.InstallPath); }
                },
                //SIT check
                new()
                {
                    Name = "SIT Installation",
                    ErrorMessage = $"Unable to find '{SIT_DLL_FILENAME}'. Install SIT before connecting.",
                    Check = () =>{ return File.Exists(Path.Combine(_configService.Config.InstallPath, "BepInEx", "plugins", SIT_DLL_FILENAME)); }
                },
                //EFT Check
                new()
                {
                    Name = "EFT Installation",
                    ErrorMessage = $"Unable to find '{EFT_EXE_FILENAME}' in the installation path.",
                    Check = () => { return File.Exists(Path.Combine(_configService.Config.InstallPath, EFT_EXE_FILENAME)); }
                },
                //Field Check
                new()
                {
                    Name = "Input Validation",
                    ErrorMessage = "Missing input field.",
                    Check = () => { return LastServer.Length > 0 && Username.Length > 0 && Password.Length > 0; }
                }
            ];

            foreach(ValidationRule rule in validationRules)
            {
                if (rule?.Check != null && !rule.Check())
                {
                    await new ContentDialog()
                    {
                        Title = rule.Name,
                        Content = rule.ErrorMessage,
                        CloseButtonText = "Ok"
                    }.ShowAsync();
                    return;
                }
            }

            //Connect to server
            string token = await LoginToServerAsync(serverAddress ?? throw new ArgumentNullException(nameof(serverAddress), "Server address is null"));
            if (string.IsNullOrEmpty(token))
                return;

            //Launch game
            Dictionary<string, string> argumentList = new()
            {
                { "-token", token },
                { "-config", JsonSerializer.Serialize(new TarkovLaunchConfig{ BackendUrl = serverAddress.AbsoluteUri }) }
            };
            string launchArguments = string.Join(' ', argumentList.Select(argument => $"{argument.Key}={argument.Value}"));
            _tarkovClientService.Start(launchArguments);

            if (_configService.Config.CloseAfterLaunch)
            {
                IApplicationLifetime? lifetime = App.Current.ApplicationLifetime;
                if (lifetime != null && lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    desktopLifetime.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

        private async Task QuickPlay()
        {
            //TODO: Find a way to meld this with the server page better, theres a lot of overlap happening here
            if(_akiServerService.IsUnhandledInstanceRunning())
            {
                await new ContentDialog()
                {
                    Title = "Unhandled Server Instance",
                    Content = "SPT-AKI is currently running. Please close any running instance of SPT-AKI.",
                    CloseButtonText = "Ok",
                }.ShowAsync();
            }
            else if(!File.Exists(_akiServerService.ExecutableFilePath))
            {
                await new ContentDialog()
                {
                    Title = "Unhandled Server Instance",
                    Content = "SPT-AKI is currently running. Please close any running instance of SPT-AKI.",
                    CloseButtonText = "Ok",
                }.ShowAsync();
            }
            else
            {
                using CancellationTokenSource cts = new();
                async void serverStartEventHandler(object? sender, EventArgs e)
                {
                    cts.Dispose();
                    await Dispatcher.UIThread.InvokeAsync(ConnectToServer);
                    _akiServerService.ServerStarted -= serverStartEventHandler;
                }

                _akiServerService.ServerStarted += serverStartEventHandler;
                _akiServerService.Start();

                cts.CancelAfter(TimeSpan.FromSeconds(30));
                cts.Token.Register(() =>
                {
                    new ContentDialog()
                    {
                        Title = "Server Error",
                        Content = "The server never started. Please check the logs for more information.",
                        CloseButtonText = "Ok"
                    }.ShowAsync();
                });
            }
        }
    }
}
