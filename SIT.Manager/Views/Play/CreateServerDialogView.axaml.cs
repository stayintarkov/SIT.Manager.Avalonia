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
        this.DataContext = dc;
        InitializeComponent();
    }

    public new Task<string> ShowAsync()
    {
        return this.ShowAsync(null).ContinueWith(t => dc.ServerCreationData ?? string.Empty);
    }
}
