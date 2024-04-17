using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;
using System.ComponentModel;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;

    private readonly string _serverUri;
    private AkiServer _server = new(new Uri("http://127.0.0.1:6969"));
    private readonly DispatcherTimer _dispatcherTimer;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private SolidColorBrush _pingColor = new(Colors.White);

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
        get => _server.Ping;
        set => SetProperty(_server.Ping, value, _server, (u, n) => u.Ping = n);
    }

    public ServerSummaryViewModel(string serverUri, ILogger<ServerSummaryViewModel> logger, IAkiServerRequestingService serverService)
    {
        _logger = logger;
        _serverService = serverService;

        _serverUri = serverUri;

        _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Normal, DispatcherTimer_Tick);
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

    protected override async void OnActivated()
    {
        base.OnActivated();
        IsLoading = true;

        try
        {
            _server = await _serverService.GetAkiServerAsync(new Uri(_serverUri));
            Name = _server.Name;
            _logger.LogDebug("{Address} found with name {Name}", Address.AbsoluteUri, Name);
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
