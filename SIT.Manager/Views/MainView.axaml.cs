using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Controls;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using SIT.Manager.Models.Messages;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SIT.Manager.Views;

public partial class MainView : ActivatableUserControl
{
    private readonly ILocalizationService _localizationService;

    public MainView()
    {
        InitializeComponent();
        _localizationService = App.Current.Services.GetRequiredService<ILocalizationService>();
        // Set the initially loaded page to be the play page and highlight this
        // in the nav view.
        // TODO ContentFrame.Navigated += ContentFrame_Navigated;
        // TODO ContentFrame.Navigate(typeof(PlayPage));
        // TODO NavView.SelectedItem = NavView.MenuItems.First();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        /* TODO
        // Make sure that the selection indicator is always right for whatever is currently displayed.
        object? selectedItem = NavView.MenuItems.FirstOrDefault(x => Type.GetType(((NavigationViewItem) x).Tag?.ToString() ?? string.Empty) == e.Content.GetType());
        if (selectedItem != null)
        {
            NavView.SelectedItem = selectedItem;
        }
        */
    }

    // I hate this so much, Please if someone knows of a better way to do this make a pull request. Even microsoft docs recommend this heathenry
    private async void NavView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        PageNavigation? pageNavigation = null;
        if (e.IsSettingsInvoked == true)
        {
            pageNavigation = new(typeof(SettingsPage));
        }
        else if (e.InvokedItemContainer != null)
        {
            string tag = e.InvokedItemContainer.Tag?.ToString() ?? string.Empty;
            if (tag == "Help")
            {
                await OpenUrlAsync("https://docs.stayintarkov.com");
            }
            else
            {
                Type? navPageType = Type.GetType(tag);
                if (navPageType != null)
                {
                    pageNavigation = new(navPageType);
                }
            }
        }

        if (pageNavigation != null)
        {
            WeakReferenceMessenger.Default.Send(new PageNavigationMessage(pageNavigation));
        }
    }

    private async Task OpenUrlAsync(string url)
    {
        try
        {
            ContentDialog contentDialog = new()
            {
                Title = _localizationService.TranslateSource("HelpTitle"),
                Content = _localizationService.TranslateSource("HelpDialogDescription", url),
                PrimaryButtonText = _localizationService.TranslateSource("ModServiceButtonYes"),
                SecondaryButtonText = _localizationService.TranslateSource("ModServiceButtonNo")
            };
            var result = await contentDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            ILogger? logger = App.Current.Services.GetService<ILogger<MainView>>();
            logger?.LogError(ex, $"Error lauching url (${url}).");
        }
    }
}
