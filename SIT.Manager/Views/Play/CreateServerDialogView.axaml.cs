using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Play;
using SIT.Manager.ViewModels.Play;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Views.Play;

public partial class CreateServerDialogView : ContentDialog
{
    private const string DEFAULT_SERVER_ADDRESS = "http://127.0.0.1:6969";

    private readonly CreateServerDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public CreateServerDialogView(ILocalizationService localizationService, bool isEdit, string serverNickname, string currentServerAddress = DEFAULT_SERVER_ADDRESS)
    {
        dc = new CreateServerDialogViewModel(currentServerAddress, serverNickname, isEdit, localizationService);
        DataContext = dc;
        InitializeComponent();
    }

    public new Task<CreateServerDialogResult> ShowAsync()
    {
        return ShowAsync(null).ContinueWith(t => new CreateServerDialogResult(t.Result, dc.ServerUri, dc.ServerNickname));
    }
}
