using Avalonia.ReactiveUI;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.ViewModels;
using System;
using System.Linq;

namespace SIT.Manager.Avalonia.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
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
        PageNavigation? pageNavigation = null;
        if (e.IsSettingsInvoked == true) {
            pageNavigation = new(typeof(SettingsPage));
        }
        else if (e.InvokedItemContainer != null) {
            Type? navPageType = Type.GetType(e.InvokedItemContainer.Tag?.ToString() ?? string.Empty);
            if (navPageType != null) {
                pageNavigation = new(navPageType);
            }
        }

        if (pageNavigation != null) {
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }
    }
}