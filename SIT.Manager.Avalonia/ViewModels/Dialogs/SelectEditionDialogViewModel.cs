using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using SIT.Manager.Avalonia.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs;

public partial class SelectEditionDialogViewModel : ViewModelBase
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
