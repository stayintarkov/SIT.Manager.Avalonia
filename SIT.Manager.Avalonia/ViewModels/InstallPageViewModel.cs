using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;
using SIT.Manager.Avalonia.Views.Installation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class InstallPageViewModel : ViewModelBase,
                                            IRecipient<ProgressInstallMessage>,
                                            IRecipient<InstallProcessStateChangedMessage>,
                                            IRecipient<InstallProcessStateRequestMessage>
{
    private readonly List<InstallStep> _installSteps = [
        new (0, typeof(SelectView), "Select"),
        new(1, typeof(ConfigureView), "Configure"),
        new(2, typeof(PatchView), "Patch"),
        new(3, typeof(InstallView), "Install"),
        new(4, typeof(CompleteView), "Complete")
    ];

    private InstallProcessState _installProcessState = new();

    [ObservableProperty]
    private int _currentInstallStep = 0;

    [ObservableProperty]
    private Control? _installStepControl;

    public ReadOnlyCollection<InstallStep> InstallationSteps => _installSteps.AsReadOnly();

    public InstallPageViewModel()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
        ResetInstallState();
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

        if (InstallationSteps.Count > CurrentInstallStep)
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
