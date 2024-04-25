using FluentAvalonia.UI.Controls;
using SIT.Manager.Models;
using SIT.Manager.Models.Play;
using SIT.Manager.ViewModels.Play;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Play;

public partial class CreateCharacterDialogView : ContentDialog
{
    private readonly CreateCharacterDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public CreateCharacterDialogView(string username, string password, bool rememberLogin, TarkovEdition[] editions)
    {
        dc = new CreateCharacterDialogViewModel(username, password, rememberLogin, editions);
        DataContext = dc;
        InitializeComponent();
    }

    public new Task<CreateCharacterDialogResult> ShowAsync()
    {
        return ShowAsync(null).ContinueWith(t => new CreateCharacterDialogResult(t.Result, dc.Username, dc.Password, dc.SaveLoginDetails, dc.SelectedEdition));
    }
}
