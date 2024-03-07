using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Avalonia.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs
{
    public partial class SelectVersionDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private GithubRelease? _selectedVersion;

        public ObservableCollection<GithubRelease> GithubReleases { get; }


        public SelectVersionDialogViewModel(List<GithubRelease> releases)
        {
            GithubReleases = new ObservableCollection<GithubRelease>(releases);
            if (GithubReleases.Any())
            {
                SelectedVersion = GithubReleases[0];
            }
        }
    }
}
