using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Avalonia.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs;

public partial class SelectEditionDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private TarkovEdition? _selectedEdition = null;

    public ObservableCollection<TarkovEdition> Editions { get; } = [];

    public SelectEditionDialogViewModel(TarkovEdition[] editions)
    {
        foreach (TarkovEdition edition in editions)
        {
            Editions.Add(edition);
        }
        SelectedEdition = Editions.FirstOrDefault();
    }
}
