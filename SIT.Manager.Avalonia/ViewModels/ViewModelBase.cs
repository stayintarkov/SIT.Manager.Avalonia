using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace SIT.Manager.Avalonia.ViewModels;

public class ViewModelBase : ObservableObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new ViewModelActivator();
}
