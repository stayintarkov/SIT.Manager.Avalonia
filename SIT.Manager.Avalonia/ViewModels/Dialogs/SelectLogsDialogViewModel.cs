using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs;
public partial class SelectLogsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private DiagnosticsOptions _selectedOptions = new();
}
