using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Installation;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

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

        _updateProgress = new Progress<double>(prog => UpdateProgressPercentage = prog);

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
            WeakReferenceMessenger.Default.Send(new InstallationRunningMessage(true));

            bool updateResult = await _appUpdaterService.Update(_updateProgress);
            if (updateResult)
            {
                _appUpdaterService.RestartApp();
            }
            else
            {
                HasError = true;
                WeakReferenceMessenger.Default.Send(new InstallationRunningMessage(false));
            }
        }
    }
}
