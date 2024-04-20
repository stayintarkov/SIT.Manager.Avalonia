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
using SIT.Manager.Views.Play;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;
    private readonly IManagerConfigService _configService;

    private readonly DispatcherTimer _dispatcherTimer;

    private AkiServer _server;

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

    public ServerSummaryViewModel(AkiServer server, ILogger<ServerSummaryViewModel> logger, IAkiServerRequestingService serverService, IManagerConfigService configService)
    {
        _logger = logger;
        _serverService = serverService;
        _configService = configService;

        _server = server;

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
        CreateServerDialogView dialog = new(Address.AbsoluteUri);
        (ContentDialogResult result, string serverUriString) = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(serverUriString))
        {
            AkiServer? server = _configService.Config.BookmarkedServers.FirstOrDefault(x => x.Address == _server.Address);
            if (server != null)
            {
                _configService.Config.BookmarkedServers.Remove(server);

                AkiServer updatedServer = new AkiServer(new Uri(serverUriString))
                {
                    Characters = server.Characters
                };
                _configService.Config.BookmarkedServers.Add(server);

                _server = updatedServer;
                _configService.UpdateConfig(_configService.Config);
            }
            else
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
            _server = await _serverService.GetAkiServerAsync(_server.Address);
            OnPropertyChanged(nameof(Name));
            _logger.LogDebug("{Address} found with name {Name}", Address.AbsoluteUri, Name);

            using (MemoryStream ms = await _serverService.GetAkiServerImage(_server, "launcher/side_scav.png"))
            {
                ServerImage = new Bitmap(ms);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't retrieve server from address {Address}", _server.Address);
            Name = "N/A";
            Ping = -2;
        }

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
