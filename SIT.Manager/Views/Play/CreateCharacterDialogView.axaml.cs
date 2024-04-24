using FluentAvalonia.UI.Controls;
using SIT.Manager.ViewModels.Play;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Play;

public partial class CreateCharacterDialogView : ContentDialog
{
    private readonly CreateCharacterDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public CreateCharacterDialogView()
    {
        dc = new CreateCharacterDialogViewModel();
        DataContext = dc;
        InitializeComponent();
    }

    public new Task<(ContentDialogResult, string, string, bool)> ShowAsync()
    {
        return ShowAsync(null).ContinueWith(t => (t.Result, dc.Username, dc.Password, dc.SaveLoginDetails));
    }
}
