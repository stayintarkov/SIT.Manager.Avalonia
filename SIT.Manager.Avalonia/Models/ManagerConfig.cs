using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace SIT.Manager.Avalonia.Models
{
    public partial class ManagerConfig : ObservableObject
    {
        [ObservableProperty]
        public string _lastServer = "http://127.0.0.1:6969";
        [ObservableProperty]
        public string _username = string.Empty;
        [ObservableProperty]
        public string _password = string.Empty;
        [ObservableProperty]
        public string _installPath = string.Empty;
        [ObservableProperty]
        public string _akiServerPath = string.Empty;
        [ObservableProperty]
        public bool _rememberLogin = false;
        [ObservableProperty]
        public bool _closeAfterLaunch = false;
        [ObservableProperty]
        public string _tarkovVersion = string.Empty;
        [ObservableProperty]
        public string _sitVersion = string.Empty;
        [ObservableProperty]
        public bool _lookForUpdates = true;
        [ObservableProperty]
        public bool _acceptedModsDisclaimer = false;
        public string ModCollectionVersion { get; set; } = string.Empty;
        public Dictionary<string, string> InstalledMods { get; set; } = [];
        [ObservableProperty]
        private Color _consoleFontColor = Colors.LightBlue;
        [ObservableProperty]
        public string _consoleFontFamily = "Consolas";
    }
}
