using FluentAvalonia.UI.Controls;
using SIT.Manager.ViewModels.Play;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Play;

public partial class CreateServerDialogView : ContentDialog
{
    private readonly CreateServerDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public CreateServerDialogView()
    {
        dc = new CreateServerDialogViewModel();
        DataContext = dc;
        InitializeComponent();
    }

    public new Task<(ContentDialogResult, string)> ShowAsync()
    {
        return ShowAsync(null).ContinueWith(t => (t.Result, dc.ServerAddress));
    }
}
