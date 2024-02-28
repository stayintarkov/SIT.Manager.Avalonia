using Avalonia.Input;
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

    /// <summary>
    /// Event that before checked what button was checked comparing the name of the button, which is stupid when it comes to translating buttons.
    /// In here we are only handing Settings Button since it's the only thing that is hardcoded here. So this is when "settings button was clicked"
    /// </summary>
    private void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.IsSettingsInvoked)
        {
            PageNavigation pageNavigation = new(typeof(SettingsPage));
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
            return;
        }
    }

    /// <summary>
    /// <see cref="PlayButton_Clicked(object?, TappedEventArgs)"/> gets fired when you click on PlayButton in Pane View.
    /// </summary>
    private void PlayButton_Clicked(object? sender, TappedEventArgs e)
    {
        PageNavigation pageNavigation = new(typeof(PlayPage));
        WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
    }

    /// <summary>
    /// <see cref="ToolsButton_Clicked(object?, TappedEventArgs)"/> gets fired when you click on ToolsButton in Pane View.
    /// </summary>
    private void ToolsButton_Clicked(object? sender, TappedEventArgs e)
    {
        PageNavigation pageNavigation = new(typeof(ToolsPage));
        WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
    }

    /// <summary>
    /// <see cref="ServerButton_Clicked(object?, TappedEventArgs)"/> gets fired when you click on ServerButton in Pane View.
    /// </summary>
    private void ServerButton_Clicked(object? sender, TappedEventArgs e)
    {
        PageNavigation pageNavigation = new(typeof(ServerPage));
        WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
    }

    /// <summary>
    /// <see cref="ModsButton_Clicked(object?, TappedEventArgs)"/> gets fired when you click on ModsButton in Pane View.
    /// </summary>
    private void ModsButton_Clicked(object? sender, TappedEventArgs e)
    {
        PageNavigation pageNavigation = new(typeof(ModsPage));
        WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
    }
}