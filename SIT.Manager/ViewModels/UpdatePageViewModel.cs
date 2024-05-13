using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Installation;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class UpdatePageViewModel : ObservableRecipient
{
    private readonly IAppUpdaterService _appUpdaterService;

    private readonly Progress<double> _updateProgress;

    [ObservableProperty]
    private double _updateProgressPercentage;

    [ObservableProperty]
    private bool _hasError = false;

    public UpdatePageViewModel(IAppUpdaterService appUpdaterService)
    {
        _appUpdaterService = appUpdaterService;

        _updateProgress = new Progress<double>(prog => UpdateProgressPercentage = prog);
    }

    private async Task DoUpdateApp()
    {
        Messenger.Send(new InstallationRunningMessage(true));
        await Task.Delay(500);

#if DEBUG
        // For debug builds don't actually allow the app to be updated and instead just mimic the action
        for (int i = 0; i < 100; i++)
        {
            UpdateProgressPercentage = i;
            await Task.Delay(Random.Shared.Next(1000));
        }
        Messenger.Send(new InstallationRunningMessage(false));
#else
        bool updateResult = await _appUpdaterService.Update(_updateProgress);
        if (updateResult)
        {
            _appUpdaterService.RestartApp();
        }
        else
        {
            HasError = true;
            Messenger.Send(new InstallationRunningMessage(false));
        }
#endif
    }

    protected override async void OnActivated()
    {
        base.OnActivated();
        await DoUpdateApp();
    }
}
