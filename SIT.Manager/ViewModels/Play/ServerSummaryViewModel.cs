using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSummaryViewModel(ILogger<ServerSummaryViewModel> logger, IAkiServerRequestingService serverService) : ObservableRecipient
{
    private readonly ILogger _logger = logger;
    private readonly IAkiServerRequestingService _serverService = serverService;

    [ObservableProperty]
    private AkiServer? _server;

    [ObservableProperty]
    private SolidColorBrush _pingColor = new(Colors.Green);

    public string ServerUri { get; set; } = string.Empty;

    protected override async void OnActivated()
    {
        base.OnActivated();

        Server = await _serverService.GetAkiServerAsync(new Uri(ServerUri));
        _logger.LogDebug($"{Server.Address.AbsoluteUri} found with name {Server.Name}");

        Server.Ping = await _serverService.GetPingAsync(Server);
        logger.LogDebug($"{Server.Name}'s ping is {Server.Ping}ms");
    }
}
