using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Models.Messages;
using SIT.Manager.Avalonia.Views.Installation;
using System;
using System.Collections.Generic;

namespace SIT.Manager.Avalonia.ViewModels;

public partial class InstallPageViewModel : ViewModelBase, IRecipient<InstallationProgressMessage>
{
    private Dictionary<int, Type> InstallPageContentViews = new() {
        { 0, typeof(SelectView) }
    };

    [ObservableProperty]
    private int _currentInstallStep = 0;

    [ObservableProperty]
    private Control _installStepControl = new TextBlock() { Text = "No Control Selected" };

    public InstallPageViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(InstallationProgressMessage message)
    {
        if (message.Value) {
            CurrentInstallStep++;
        }
        else {
            CurrentInstallStep--;
        }

        if (InstallPageContentViews.TryGetValue(CurrentInstallStep, out Type? value)) {

        }
    }
}
