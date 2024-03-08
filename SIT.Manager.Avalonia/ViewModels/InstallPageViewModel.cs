using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Installation;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Views.Installation;
using System;
using System.Collections.Generic;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class InstallPageViewModel : ViewModelBase, IRecipient<InstallationProgressMessage>
{
    private readonly Dictionary<int, Type> InstallPageContentViews = new() {
        { 0, typeof(SelectView) },
        { 1, typeof(ConfigureView) },
        { 2, typeof(PatchView) },
        { 3, typeof(InstallView) },
        { 4, typeof(CompleteView) }
    };

    private InstallProcessState _install = new InstallProcessState();

    [ObservableProperty]
    private int _currentInstallStep = 0;

    [ObservableProperty]
    private Control _installStepControl = new SelectView();

    public InstallPageViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
        ResetInstallState();
    }

    private void ResetInstallState()
    {
        CurrentInstallStep = 0;
        InstallStepControl = new SelectView();
        _install = new InstallProcessState();
    }

    public void Receive(InstallationProgressMessage message)
    {
        if (message.Value)
        {
            CurrentInstallStep++;
        }
        else
        {
            CurrentInstallStep--;
        }

        if (InstallPageContentViews.TryGetValue(CurrentInstallStep, out Type? value))
        {
            InstallStepControl = (Control) (System.Activator.CreateInstance(value) ?? new TextBlock() { Text = "No Control Selected" });
        }
        else
        {
            ResetInstallState();
        }
    }
}
