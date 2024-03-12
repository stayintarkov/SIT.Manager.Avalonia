using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Views.Dialogs;

public partial class SelectEditionDialog : ContentDialog
{
    private readonly SelectEditionDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public SelectEditionDialog(TarkovEdition[] editions)
    {
        dc = new SelectEditionDialogViewModel(editions);
        this.DataContext = dc;
        InitializeComponent();
    }

    public new Task<TarkovEdition> ShowAsync()
    {
        return this.ShowAsync(null).ContinueWith(t => dc.SelectedEdition ?? new TarkovEdition("Edge Of Darkness"));
    }
}
