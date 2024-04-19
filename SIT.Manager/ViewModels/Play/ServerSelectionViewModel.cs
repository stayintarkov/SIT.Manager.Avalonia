using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ManagedProcess;
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
            ServerList.Add(ActivatorUtilities.CreateInstance<ServerSummaryViewModel>(_serviceProvider, serverUriString));

            bool addedSuccessfully = _configService.Config.BookmarkedServers.TryAdd(serverUriString, []);
            if (addedSuccessfully)
            {
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
        foreach (string server in _configService.Config.BookmarkedServers.Keys)
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

                _configService.Config.BookmarkedServers.Remove(server.Address.OriginalString);
                _configService.UpdateConfig(_configService.Config);
            }
        }
    }
}
