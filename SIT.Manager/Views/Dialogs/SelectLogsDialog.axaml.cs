using FluentAvalonia.UI.Controls;
using SIT.Manager.Models;
using SIT.Manager.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Dialogs;

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

    public new Task<(ContentDialogResult, DiagnosticsOptions)> ShowAsync()
    {
        return this.ShowAsync(null).ContinueWith(t => (t.Result, dc.SelectedOptions));
    }
}
