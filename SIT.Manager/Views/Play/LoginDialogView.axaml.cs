using FluentAvalonia.UI.Controls;
using SIT.Manager.ViewModels.Play;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Play;

public partial class LoginDialogView : ContentDialog
{
    private readonly LoginDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public LoginDialogView(string username)
    {
        dc = new LoginDialogViewModel(username);
        DataContext = dc;

        InitializeComponent();
    }

    public new Task<(ContentDialogResult, string, bool)> ShowAsync()
    {
        return ShowAsync(null).ContinueWith(t => (t.Result, dc.Password, dc.RememberMe));
    }
}
