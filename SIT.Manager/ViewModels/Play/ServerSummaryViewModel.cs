using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Services.Caching;
using SIT.Manager.Views.Play;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel : ObservableRecipient
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<ServerSummaryViewModel> _logger;
    private readonly IAkiServerRequestingService _serverService;
    private readonly IManagerConfigService _configService;
    private readonly ICachingService _cachingService;

    private readonly DispatcherTimer _dispatcherTimer;

    private AkiServer _server;
    private readonly List<AkiServer> _bookmarkedServers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShownAddress))]
    private bool _showIPOverride = false;

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

    public string ShownAddress
    {
        get
        {
            string ret = new('*', Address.AbsoluteUri.Length);
            if (!_configService.Config.LauncherSettings.HideIpAddress || ShowIPOverride)
            {
                ret = Address.AbsoluteUri;
            }

            return ret;
        }
    }

    public string ServerDisplayName => !string.IsNullOrEmpty(_server?.Nickname) ? _server.Nickname : Name;

    public string Name
    {
        get => _server.Name;
        set
        {
            SetProperty(_server.Name, value, _server, (u, n) => u.Name = n);
            OnPropertyChanged(nameof(ServerDisplayName));
        }
    }

    public int Ping
    {
        get => _server?.Ping ?? -3;
        set => SetProperty(_server.Ping, value, _server, (u, n) => u.Ping = n);
    }

    public IAsyncRelayCommand EditCommand { get; }

    public ServerSummaryViewModel(
        AkiServer server,
        ILocalizationService localizationService,
        ILogger<ServerSummaryViewModel> logger,
        IAkiServerRequestingService serverService,
        IManagerConfigService configService,
        ICachingService cachingService)
    {
        _localizationService = localizationService;
        _logger = logger;
        _serverService = serverService;
        _configService = configService;
        _cachingService = cachingService;

        _server = server;
        _bookmarkedServers = _configService.Config.SITSettings.BookmarkedServers;

        EditCommand = new AsyncRelayCommand(Edit);

        _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Normal, DispatcherTimer_Tick);
        Task.Run(() =>
        {
            System.Threading.Thread.Sleep(2000);
            Dispatcher.UIThread.Invoke(() => DispatcherTimer_Tick(null, new EventArgs()));
        });
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
        CreateServerDialogView dialog = new(_localizationService, true, _server?.Nickname ?? string.Empty, Address.AbsoluteUri);
        CreateServerDialogResult result = await dialog.ShowAsync();
        if (result.DialogResult == ContentDialogResult.Primary)
        {
            if (_server != null)
            {
                _configService.Config.BookmarkedServers.RemoveAll(x => x.Address == _server.Address);

                AkiServer updatedServer = new(result.ServerUri)
                {
                    Characters = _server.Characters,
                    Name = _server.Name,
                    Nickname = result.ServerNickname,
                    Ping = _server.Ping
                };
                _bookmarkedServers.Add(updatedServer);

                _server = updatedServer;

                await RefreshServerData();
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ServerSummaryViewModelEditDialogTitle"),
                    Content = _localizationService.TranslateSource("ServerSummaryViewModelEditDialogContent"),
                    PrimaryButtonText = _localizationService.TranslateSource("ServerSummaryViewModelEditDialogPrimaryButtonText")
                };
                await contentDialog.ShowAsync();
            }
        }
    }

    private async void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        _dispatcherTimer.Stop();

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

        if (IsActive)
        {
            _dispatcherTimer.Start();
        }
    }

    private void UpdatePingColor()
    {
        PingColor = Ping switch
        {
            <= 0 => new SolidColorBrush(Colors.White),
            <= 50 => new SolidColorBrush(Colors.Green),
            <= 150 => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Red),
        };
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        IsLoading = true;

        await RefreshServerData();

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

    public async Task RefreshServerData()
    {
        _dispatcherTimer.Stop();

        try
        {
            AkiServer updatedServer = await _serverService.GetAkiServerAsync(_server.Address);
            _server = new(updatedServer.Address)
            {
                Characters = updatedServer.Characters,
                Name = updatedServer.Name,
                Nickname = _server.Nickname
            };
            _logger.LogDebug("{Address} found with name {Name}", Address.AbsoluteUri, Name);

            // Ensure the properties get updated
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(ShownAddress));
            OnPropertyChanged(nameof(ServerDisplayName));

            string serverImageCacheKey = $"{_server.Address.Host}:{_server.Address.Port} serverimg";
            CacheValue<Bitmap> cachedImage = await _cachingService.InMemory.GetOrComputeAsync(serverImageCacheKey, async (key) =>
            {
                using (MemoryStream ms = await _serverService.GetAkiServerImage(_server, "launcher/side_scav.png"))
                {
                    _logger.LogDebug("Creating new cached bitmap for key \"{key}\"", key);
                    return new Bitmap(ms);
                }
            }, TimeSpan.FromHours(1));
            ServerImage = cachedImage.Value;
            _dispatcherTimer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't retrieve server from address {Address}", _server.Address);
            Name = _localizationService.TranslateSource("ServerSummaryViewModelNoServerNameText");
            Ping = -2;
            _dispatcherTimer.Stop();
        }

        Dispatcher.UIThread.Invoke(() => DispatcherTimer_Tick(null, new EventArgs()));
    }
}
