using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using SIT.Manager.Models.Play;
using SIT.Manager.Views.Play;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class ServerSelectionViewModel : ObservableRecipient, IRecipient<DeleteServerMessage>
{
    private readonly IManagerConfigService _configService;
    private readonly ILocalizationService _localizationService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<ServerSummaryViewModel> ServerList { get; } = [];

    public IAsyncRelayCommand CreateServerCommand { get; }

    public ServerSelectionViewModel(IServiceProvider serviceProvider, ILocalizationService localizationService, IManagerConfigService configService)
    {
        _configService = configService;
        _localizationService = localizationService;
        _serviceProvider = serviceProvider;

        CreateServerCommand = new AsyncRelayCommand(CreateServer);
    }

    private async Task CreateServer()
    {
        CreateServerDialogView dialog = new();
        (ContentDialogResult result, Uri serverUri) = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            bool serverExists = _configService.Config.BookmarkedServers
                .Any(x => Uri.Compare(x.Address, serverUri, UriComponents.HostAndPort, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0);
            if (!serverExists)
            {
                AkiServer newServer = new(serverUri);
                ServerList.Add(ActivatorUtilities.CreateInstance<ServerSummaryViewModel>(_serviceProvider, newServer));
                _configService.Config.BookmarkedServers.Add(newServer);
                _configService.UpdateConfig(_configService.Config);
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    Title = _localizationService.TranslateSource("ServerSelectionViewModelAddServerDialogTitle"),
                    Content = _localizationService.TranslateSource("ServerSelectionViewModelAddServerDialogContent"),
                    PrimaryButtonText = _localizationService.TranslateSource("ServerSelectionViewModelAddServerPrimaryButtonText")
                };
                await contentDialog.ShowAsync();
            }
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        foreach (AkiServer server in _configService.Config.BookmarkedServers)
        {
            //This has the potential to be kinda slow on *large* sets. If so we can swap to a hashset but that feels overkill rn
            if (!ServerList.Where(x => Uri.Compare(x.Address, server.Address, UriComponents.HostAndPort, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0).Any())
                ServerList.Add(ActivatorUtilities.CreateInstance<ServerSummaryViewModel>(_serviceProvider, server));
        }
    }

    public async void Receive(DeleteServerMessage message)
    {
        ContentDialog contentDialog = new()
        {
            Title = _localizationService.TranslateSource("ServerSelectionViewModelDeleteServerDialogTitle"),
            Content = _localizationService.TranslateSource("ServerSelectionViewModelDeleteServerDialogContent"),
            PrimaryButtonText = _localizationService.TranslateSource("ServerSelectionViewModelDeleteServerPrimaryButtonText"),
            CloseButtonText = _localizationService.TranslateSource("ServerSelectionViewModelDeleteServerCloseButtonText")
        };
        ContentDialogResult result = await contentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ServerSummaryViewModel? server = ServerList.FirstOrDefault(x => x.Address == message.Value);
            if (server != null)
            {
                ServerList.Remove(server);

                AkiServer? serverToRemove = _configService.Config.BookmarkedServers.Find(x => x.Address == server.Address);
                if (serverToRemove != null)
                {
                    _configService.Config.BookmarkedServers.Remove(serverToRemove);
                    _configService.UpdateConfig(_configService.Config);
                }
            }
        }
    }
}
