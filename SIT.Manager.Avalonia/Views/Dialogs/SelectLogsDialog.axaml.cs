using Avalonia.Controls;
using Avalonia.Labs.Controls;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Views.Dialogs;
public partial class SelectLogsDialog : ContentDialog
{
    readonly SelectLogsDialogViewModel dc;
    protected override Type StyleKeyOverride => typeof(ContentDialog);
    public SelectLogsDialog()
    {
        dc = new SelectLogsDialogViewModel();
        this.DataContext = dc;
        InitializeComponent();
    }

    public new Task<DiagnosticsOptions> ShowAsync()
    {
        return this.ShowAsync(null).ContinueWith(t => dc.SelectedOptions);
    }
}
