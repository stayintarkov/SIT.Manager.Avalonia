using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Avalonia.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase, IRecipient<PageNavigationMessage>
{
    private readonly IActionNotificationService _actionNotificationService;
    private readonly IBarNotificationService _barNotificationService;

    private Frame? contentFrame;

    [ObservableProperty]
    private ActionNotification? _actionPanelNotification = new(string.Empty, 0, false);

    public ObservableCollection<BarNotification> BarNotifications { get; } = [];

    public IAsyncRelayCommand UpdateButtonCommand { get; }

    public MainViewModel(IActionNotificationService actionNotificationService, IBarNotificationService barNotificationService) {
        _actionNotificationService = actionNotificationService;
        _barNotificationService = barNotificationService;

        _actionNotificationService.ActionNotificationReceived += ActionNotificationService_ActionNotificationReceived;
        _barNotificationService.BarNotificationReceived += BarNotificationService_BarNotificationReceived;

        WeakReferenceMessenger.Default.Register(this);

        UpdateButtonCommand = new AsyncRelayCommand(UpdateButton);
    }

    private async Task UpdateButton()
    {
        ContentDialogResult updateResult = await new ContentDialog()
        {
            Title = "Update Confirmation",
            Content = "Are you sure you want to update? This will close the manager to perform an update.",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No"
        }.ShowAsync();

        if(updateResult == ContentDialogResult.Primary)
        {
            //TODO: Add a way to update for linux users
            if (OperatingSystem.IsWindows())
            {
                //TODO: Change this to use a const
                string updaterPath = Path.Combine(AppContext.BaseDirectory, "SIT.Manager.Updater.exe");
                if (File.Exists(updaterPath))
                {
                    Process.Start(updaterPath);
                    IApplicationLifetime? lifetime = Application.Current?.ApplicationLifetime;
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
            else
            {
                await new ContentDialog()
                {
                    Title = "Unsupported",
                    Content = "Automatic updating isn't currently available for Linux.",
                    CloseButtonText = "Ok"
                }.ShowAsync();
            }
        }
    }

    private void ActionNotificationService_ActionNotificationReceived(object? sender, ActionNotification e) {
        ActionPanelNotification = e;
    }

    private async void BarNotificationService_BarNotificationReceived(object? sender, BarNotification e) {
        BarNotifications.Add(e);
        if (e.Delay > 0) {
            await Task.Delay(TimeSpan.FromSeconds(e.Delay));
            BarNotifications.Remove(e);
        }
    }

    private bool NavigateToPage(Type page, bool suppressTransition = false) {
        object? currentPage = contentFrame?.Content;
        if (page == currentPage?.GetType()) {
            return false;
        }
        return contentFrame?.Navigate(page, null, suppressTransition ? new SuppressNavigationTransitionInfo() : null) ?? false;
    }

    public void RegisterContentFrame(Frame frame) {
        contentFrame = frame;
    }

    public void Receive(PageNavigationMessage message) {
        NavigateToPage(message.Value.TargetPage, message.Value.SuppressTransition);
    }
}
