using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models;

namespace SIT.Manager.ViewModels.Dialogs;

public partial class SelectLogsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private DiagnosticsOptions _selectedOptions = new();
}
