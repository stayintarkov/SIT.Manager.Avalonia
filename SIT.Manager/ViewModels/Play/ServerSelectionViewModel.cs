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
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<ServerSummaryViewModel> ServerList { get; } = [];

    public IAsyncRelayCommand CreateServerCommand { get; }

    public ServerSelectionViewModel(IServiceProvider serviceProvider, IManagerConfigService configService)
    {
        _configService = configService;
        _serviceProvider = serviceProvider;

        CreateServerCommand = new AsyncRelayCommand(CreateServer);
    }

    private async Task CreateServer()
    {
        CreateServerDialogView dialog = new();
        (ContentDialogResult result, string serverUriString) = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(serverUriString))
        {
            bool serverExists = _configService.Config.BookmarkedServers.Any(x => x.Address.OriginalString == serverUriString);
            if (!serverExists)
            {
                AkiServer newServer = new AkiServer(new Uri(serverUriString));
                ServerList.Add(ActivatorUtilities.CreateInstance<ServerSummaryViewModel>(_serviceProvider, newServer));
                _configService.Config.BookmarkedServers.Add(newServer);
                _configService.UpdateConfig(_configService.Config);
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    Title = "Add Server Error",
                    Content = "Failed to add server as it already exists",
                    PrimaryButtonText = "Ok"
                };
                await contentDialog.ShowAsync();
            }
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        ServerList.Clear();
        foreach (AkiServer server in _configService.Config.BookmarkedServers)
        {
            ServerList.Add(ActivatorUtilities.CreateInstance<ServerSummaryViewModel>(_serviceProvider, server));
        }
    }

    public async void Receive(DeleteServerMessage message)
    {
        ContentDialog contentDialog = new()
        {
            Title = "Delete Server",
            Content = "Are you sure you want to delete this server?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No"
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
