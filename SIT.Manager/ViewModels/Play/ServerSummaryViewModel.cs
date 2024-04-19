using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Views.Play;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;
    private readonly IManagerConfigService _configService;

    private readonly DispatcherTimer _dispatcherTimer;

    private string _serverUri;
    private AkiServer _server = new(new Uri("http://127.0.0.1:6969"));

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private SolidColorBrush _pingColor = new(Colors.White);

    [ObservableProperty]
    private Bitmap? _serverImage;

    public Uri Address
    {
        get => _server.Address;
    }

    public string Name
    {
        get => _server.Name;
        set => SetProperty(_server.Name, value, _server, (u, n) => u.Name = n);
    }

    public int Ping
    {
        get => _server?.Ping ?? -3;
        set => SetProperty(_server.Ping, value, _server, (u, n) => u.Ping = n);
    }

    public IAsyncRelayCommand EditCommand { get; }

    public ServerSummaryViewModel(string serverUri, ILogger<ServerSummaryViewModel> logger, IAkiServerRequestingService serverService, IManagerConfigService configService)
    {
        _logger = logger;
        _serverService = serverService;
        _configService = configService;

        _serverUri = serverUri;

        EditCommand = new AsyncRelayCommand(Edit);

        _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Normal, DispatcherTimer_Tick);
    }

    [RelayCommand]
    private void Connect()
    {
        WeakReferenceMessenger.Default.Send(new ServerConnectMessage(_server));
    }

    [RelayCommand]
    private void Delete()
    {
        WeakReferenceMessenger.Default.Send(new DeleteServerMessage(Address));
    }

    private async Task Edit()
    {
        bool hasEditError = false;

        CreateServerDialogView dialog = new();
        (ContentDialogResult result, string serverUriString) = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(serverUriString))
        {
            bool gotCurrentEntry = _configService.Config.BookmarkedServers.TryGetValue(_serverUri, out List<AkiCharacter>? characterList);
            if (!gotCurrentEntry || characterList == null)
            {
                hasEditError = true;
            }
            else
            {
                bool addedSuccessfully = _configService.Config.BookmarkedServers.TryAdd(serverUriString, characterList);
                if (addedSuccessfully)
                {
                    _configService.Config.BookmarkedServers.Remove(_serverUri);
                    _configService.UpdateConfig(_configService.Config);
                    _serverUri = serverUriString;
                }
                else
                {
                    hasEditError = true;
                }
            }
        }

        if (hasEditError)
        {
            ContentDialog contentDialog = new()
            {
                Title = "Edit Server Error",
                Content = "Failed to edit server",
                PrimaryButtonText = "Ok"
            };
            await contentDialog.ShowAsync();
        }
    }

    private async void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            Ping = await _serverService.GetPingAsync(_server);
            _logger.LogDebug("{Name}'s ping is {Ping}ms", Name, Ping);
        }
        catch (Exception ex)
        {
            Ping = -1;
            _logger.LogWarning(ex, "Couldn't evaluate ping from server {Name}", Name);
        }
    }

    private void UpdatePingColor()
    {
        if (Ping < 0)
        {
            PingColor = new SolidColorBrush(Colors.White);
        }
        else if (Ping < 50)
        {
            PingColor = new SolidColorBrush(Colors.Green);
        }
        else if (Ping < 150)
        {
            PingColor = new SolidColorBrush(Colors.Orange);
        }
        else
        {
            PingColor = new SolidColorBrush(Colors.Red);
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        IsLoading = true;

        try
        {
            _server = await _serverService.GetAkiServerAsync(new Uri(_serverUri));
            OnPropertyChanged(nameof(Name));
            _logger.LogDebug("{Address} found with name {Name}", Address.AbsoluteUri, Name);

            using (MemoryStream ms = await _serverService.GetAkiServerImage(_server, "launcher/side_scav.png"))
            {
                ServerImage = new Bitmap(ms);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't retrieve server from address {Address}", _serverUri);
            _server = new AkiServer(new Uri(_serverUri));
            Name = "N/A";
        }

        Ping = -2;

        _dispatcherTimer.Start();
        IsLoading = false;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        _dispatcherTimer.Stop();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(Ping))
        {
            UpdatePingColor();
        }
    }
}
