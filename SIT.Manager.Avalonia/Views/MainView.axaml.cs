using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIT.Manager.Avalonia.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    private static readonly Dictionary<string, Type> NavMenuLookup = new()
    {
        { "Play", typeof(PlayPage) },
        { "Tools", typeof(ToolsPage) },
        { "Server", typeof(ServerPage) },
        { "Mods", typeof(ModsPage) },
        { "Settings", typeof(SettingsPage) },
    };

    public MainView() {
        InitializeComponent();

        // Set the initially loaded page to be the play page and highlight this
        // in the nav view.
        ContentFrame.Navigate(typeof(PlayPage));
        NavView.SelectedItem = NavView.MenuItems.First();

        // MainViewModel's WhenActivated block will also get called.
        this.WhenActivated(disposables => {
            /* Handle view activation etc. */
            if (DataContext is MainViewModel dataContext) {
                // Register the content frame so that we can update it from the view model
                dataContext.RegisterContentFrame(ContentFrame);
            }
        });

    }

    // I hate this so much, Please if someone knows of a better way to do this make a pull request. Even microsoft docs recommend this heathenry
    private void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e) {
        string requestedPage = e.InvokedItem.ToString() ?? string.Empty;
        if (NavMenuLookup.TryGetValue(requestedPage, out Type? page)) {
            PageNavigation pageNavigation = new(page);
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }
    }
}
