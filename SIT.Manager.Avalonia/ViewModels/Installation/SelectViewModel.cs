using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;

namespace SIT.Manager.Avalonia.ViewModels.Installation
{
    public partial class SelectViewModel(IManagerConfigService configsService) : ViewModelBase
    {
        [ObservableProperty]
        private ManagerConfig _config = configsService.Config;

        [RelayCommand]
        private void Progress()
        {
            WeakReferenceMessenger.Default.Send(new InstallationProgressMessage(true));
        }
    }
}
