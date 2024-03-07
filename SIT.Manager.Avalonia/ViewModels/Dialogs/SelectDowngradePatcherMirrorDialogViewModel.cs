using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs
{
    public partial class SelectDowngradePatcherMirrorDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        private KeyValuePair<string, string>? _selectedMirror;

        public ObservableCollection<KeyValuePair<string, string>> AvailableMirrors { get; }

        public SelectDowngradePatcherMirrorDialogViewModel(Dictionary<string, string> mirrors)
        {
            AvailableMirrors = new(mirrors);
            if (AvailableMirrors.Any())
            {
                SelectedMirror = AvailableMirrors[0];
            }
        }
    }
}
