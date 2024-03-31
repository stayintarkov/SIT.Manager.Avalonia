using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using SIT.Manager.Controls;
using SIT.Manager.Models;
using SIT.Manager.Models.Messages;
using SIT.Manager.ViewModels;
using System;
using System.Linq;

namespace SIT.Manager.Views;

public partial class MainView : ActivatableUserControl
{
    public MainView()
    {
        InitializeComponent();

        throw new Exception();

        // Set the initially loaded page to be the play page and highlight this
        // in the nav view.
        ContentFrame.Navigated += ContentFrame_Navigated;
        ContentFrame.Navigate(typeof(PlayPage));
        NavView.SelectedItem = NavView.MenuItems.First();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Make sure that the selection indicator is always right for whatever is currently displayed.
        object? selectedItem = NavView.MenuItems.FirstOrDefault(x => Type.GetType(((NavigationViewItem) x).Tag?.ToString() ?? string.Empty) == e.Content.GetType());
        if (selectedItem != null)
        {
            NavView.SelectedItem = selectedItem;
        }
    }

    // I hate this so much, Please if someone knows of a better way to do this make a pull request. Even microsoft docs recommend this heathenry
    private void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        PageNavigation? pageNavigation = null;
        if (e.IsSettingsInvoked == true)
        {
            pageNavigation = new(typeof(SettingsPage));
        }
        else if (e.InvokedItemContainer != null)
        {
            Type? navPageType = Type.GetType(e.InvokedItemContainer.Tag?.ToString() ?? string.Empty);
            if (navPageType != null)
            {
                pageNavigation = new(navPageType);
            }
        }

        if (pageNavigation != null)
        {
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }
    }

    protected override void OnActivated()
    {
        if (DataContext is MainViewModel dataContext)
        {
            // Register the content frame so that we can update it from the view model
            dataContext.RegisterContentFrame(ContentFrame);
        }
    }
}
