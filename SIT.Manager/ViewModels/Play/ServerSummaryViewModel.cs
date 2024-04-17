using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IAkiServerRequestingService _serverService;

    private readonly string _serverUri;
    private readonly DispatcherTimer _dispatcherTimer;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private AkiServer? _server;

    [ObservableProperty]
    private SolidColorBrush _pingColor = new(Colors.White);

    public ServerSummaryViewModel(string serverUri, ILogger<ServerSummaryViewModel> logger, IAkiServerRequestingService serverService)
    {
        _logger = logger;
        _serverService = serverService;

        _serverUri = serverUri;

        _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Normal, DispatcherTimer_Tick);
    }

    private void UpdatePingColor()
    {
        int ping = -1;
        if (Server != null)
        {
            ping = Server.Ping;
        }

        if (ping < 0)
        {
            PingColor = new SolidColorBrush(Colors.White);
        }
        else if (ping < 50)
        {
            PingColor = new SolidColorBrush(Colors.Green);
        }
        else if (ping < 150)
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
        if (Server != null)
        {
            try
            {
                Server.Ping = await _serverService.GetPingAsync(Server);
                _logger.LogDebug("{Name}'s ping is {Ping}ms", Server.Name, Server.Ping);
            }
            catch (Exception ex)
            {
                Server.Ping = -1;
                _logger.LogWarning(ex, "Couldn't evaluate ping from server {Name}", Server.Name);
            }
            UpdatePingColor();
        }
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        IsLoading = true;

        try
        {
            Server = await _serverService.GetAkiServerAsync(new Uri(_serverUri));
            _logger.LogDebug("{Address} found with name {Name}", Server.Address.AbsoluteUri, Server.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't retrieve server from address {Address}", _serverUri);
            Server = new AkiServer(new Uri(_serverUri))
            {
                Name = "N/A",
                Ping = -1
            };
        }

        _dispatcherTimer.Start();
        IsLoading = false;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        _dispatcherTimer.Stop();
    }
}
