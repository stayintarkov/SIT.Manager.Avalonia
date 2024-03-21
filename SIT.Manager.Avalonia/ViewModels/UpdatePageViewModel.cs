using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Interfaces;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class UpdatePageViewModel : ObservableObject
{
    private readonly IAppUpdaterService _appUpdaterService;
    private readonly ILocalizationService _localizationService;

    private readonly Progress<double> _updateProgress;

    [ObservableProperty]
    private double _updateProgressPercentage;

    [ObservableProperty]
    private bool _hasError = false;

    public IAsyncRelayCommand UpdateManagerCommand { get; }

    public UpdatePageViewModel(IAppUpdaterService appUpdaterService, ILocalizationService localizationService)
    {
        _appUpdaterService = appUpdaterService;
        _localizationService = localizationService;

        _updateProgress = new Progress<double>(prog => UpdateProgressPercentage = prog * 100);

        UpdateManagerCommand = new AsyncRelayCommand(UpdateManager);
    }

    private async Task UpdateManager()
    {
        ContentDialogResult updateRequestResult = await new ContentDialog()
        {
            Title = _localizationService.TranslateSource("UpdatePageViewModelUpdateConfirmationTitle"),
            Content = _localizationService.TranslateSource("UpdatePageViewModelUpdateConfirmationDescription"),
            PrimaryButtonText = _localizationService.TranslateSource("UpdatePageViewModelButtonYes"),
            CloseButtonText = _localizationService.TranslateSource("UpdatePageViewModelButtonNo")
        }.ShowAsync();

        if (updateRequestResult == ContentDialogResult.Primary)
        {
            // TODO disable users being able to navigate until we have finished updating and restarted (or there is an error)
            bool updateResult = await _appUpdaterService.Update(_updateProgress);
            if (updateResult)
            {
                _appUpdaterService.RestartApp();
            }
            else
            {
                HasError = true;
            }
        }
    }
}
