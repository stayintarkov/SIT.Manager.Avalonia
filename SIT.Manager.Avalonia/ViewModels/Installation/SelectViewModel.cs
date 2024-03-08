using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.Models.Messages;

namespace SIT.Manager.Avalonia.ViewModels.Installation
{
    public partial class SelectViewModel : ViewModelBase
    {
        private readonly IManagerConfigService _configService;
        private readonly IInstallerService _installerService;

        [ObservableProperty]
        private ManagerConfig _config;

        [ObservableProperty]
        private bool _detectedBSGInstallPath = false;

        public SelectViewModel(IManagerConfigService configsService, IInstallerService installerService)
        {
            _configService = configsService;
            _installerService = installerService;

            Config = _configService.Config;
            if (string.IsNullOrEmpty(Config.InstallPath))
            {
                string detectedInstallPath = _installerService.GetEFTInstallPath();
                if (!string.IsNullOrEmpty(detectedInstallPath))
                {
                    Config.InstallPath = detectedInstallPath;
                    DetectedBSGInstallPath = true;
                }
            }
        }

        [RelayCommand]
        private void Progress()
        {
            WeakReferenceMessenger.Default.Send(new InstallationProgressMessage(true));
        }
    }
}
