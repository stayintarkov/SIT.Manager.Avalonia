using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Installation;
using SIT.Manager.Views.Installation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIT.Manager.ViewModels;

public partial class InstallPageViewModel : ObservableRecipient,
                                            IRecipient<ProgressInstallMessage>,
                                            IRecipient<InstallProcessStateChangedMessage>,
                                            IRecipient<InstallProcessStateRequestMessage>
{
    private List<InstallStep> _sitInstallSteps;
    private List<InstallStep> _serverInstallSteps;
    private readonly ILocalizationService _localizationService;

    private InstallProcessState _installProcessState = new();

    private bool _usingSitInstallSteps = true;

    [ObservableProperty]
    private int _currentInstallStep = 0;

    [ObservableProperty]
    private Control? _installStepControl;

    [ObservableProperty]
    private ReadOnlyCollection<InstallStep> _installationSteps;

    public InstallPageViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _localizationService.LocalizationChanged += (o, e) =>
        {

            _sitInstallSteps =
            [
                new(typeof(SelectView), _localizationService.TranslateSource("InstallPageViewModelSelectText")),
                new(typeof(ConfigureSitView), _localizationService.TranslateSource("InstallPageViewModelConfigureText")),
                new(typeof(PatchView), _localizationService.TranslateSource("InstallPageViewModelPatchText")),
                new(typeof(InstallView), _localizationService.TranslateSource("InstallPageViewModelInstallText")),
                new(typeof(CompleteView), _localizationService.TranslateSource("InstallPageViewModelCompleteText"))
            ];
            _serverInstallSteps =
            [
                new(typeof(SelectView), _localizationService.TranslateSource("InstallPageViewModelSelectText")),
                new(typeof(ConfigureServerView), _localizationService.TranslateSource("InstallPageViewModelConfigureText")),
                new(typeof(InstallView), _localizationService.TranslateSource("InstallPageViewModelInstallText")),
                new(typeof(CompleteView), _localizationService.TranslateSource("InstallPageViewModelCompleteText"))
            ];
            InstallationSteps = _sitInstallSteps.AsReadOnly();
        };
        WeakReferenceMessenger.Default.RegisterAll(this);
        _sitInstallSteps =
        [
            new(typeof(SelectView), _localizationService.TranslateSource("InstallPageViewModelSelectText")),
            new(typeof(ConfigureSitView), _localizationService.TranslateSource("InstallPageViewModelConfigureText")),
            new(typeof(PatchView), _localizationService.TranslateSource("InstallPageViewModelPatchText")),
            new(typeof(InstallView), _localizationService.TranslateSource("InstallPageViewModelInstallText")),
            new(typeof(CompleteView), _localizationService.TranslateSource("InstallPageViewModelCompleteText"))
        ];
        _serverInstallSteps =
        [
            new(typeof(SelectView), _localizationService.TranslateSource("InstallPageViewModelSelectText")),
            new(typeof(ConfigureServerView), _localizationService.TranslateSource("InstallPageViewModelConfigureText")),
            new(typeof(InstallView), _localizationService.TranslateSource("InstallPageViewModelInstallText")),
            new(typeof(CompleteView), _localizationService.TranslateSource("InstallPageViewModelCompleteText"))
        ];
        InstallationSteps = _sitInstallSteps.AsReadOnly();
        ResetInstallState();
    }

    private void AdjustInstallSteps()
    {
        // Cache the current install steps just in case we update the steps we want to go back to what we displayed before
        int currentInstallStep = CurrentInstallStep;
        if (_installProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallSit || _installProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateSit)
        {
            if (!_usingSitInstallSteps)
            {
                InstallationSteps = _sitInstallSteps.AsReadOnly();
                _usingSitInstallSteps = true;
            }
        }
        else if (_installProcessState.RequestedInstallOperation == RequestedInstallOperation.InstallServer || _installProcessState.RequestedInstallOperation == RequestedInstallOperation.UpdateServer)
        {
            if (_usingSitInstallSteps)
            {
                InstallationSteps = _serverInstallSteps.AsReadOnly();
                _usingSitInstallSteps = false;
            }
        }

        if (CurrentInstallStep < 0)
        {
            CurrentInstallStep = currentInstallStep;
        }
    }

    private void ResetInstallState()
    {
        CurrentInstallStep = 0;
        InstallStepControl = new SelectView();
        _installProcessState = new InstallProcessState();
    }

    public void Receive(ProgressInstallMessage message)
    {
        if (message.Value)
        {
            CurrentInstallStep++;
        }
        else
        {
            CurrentInstallStep--;
        }

        // Depending on what kind of install we are doing
        // we may have to adjust the steps we are taking.
        AdjustInstallSteps();

        if (InstallationSteps.Count >= CurrentInstallStep && CurrentInstallStep >= 0)
        {
            Type value = InstallationSteps[CurrentInstallStep].InstallationView;
            InstallStepControl = (Control) (System.Activator.CreateInstance(value) ?? new TextBlock() { Text = "No Control Selected" });
        }
        else
        {
            ResetInstallState();
        }
    }

    public void Receive(InstallProcessStateChangedMessage message)
    {
        _installProcessState = message.Value;
    }

    public void Receive(InstallProcessStateRequestMessage message)
    {
        message.Reply(_installProcessState);
    }
}
