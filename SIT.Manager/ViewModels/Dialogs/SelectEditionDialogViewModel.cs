using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Extentions;
using SIT.Manager.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIT.Manager.ViewModels.Dialogs;

public partial class SelectEditionDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private TarkovEdition? _selectedEdition = null;

    public ObservableCollection<TarkovEdition> Editions { get; } = [];

    public SelectEditionDialogViewModel(TarkovEdition[] editions)
    {
        Editions.AddRange(editions);
        SelectedEdition = Editions.FirstOrDefault();
    }
}
