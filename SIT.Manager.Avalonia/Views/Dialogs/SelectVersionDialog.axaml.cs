using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Views.Dialogs;

public partial class SelectVersionDialog : ContentDialog
{
    private readonly SelectVersionDialogViewModel dc;

    protected override Type StyleKeyOverride => typeof(ContentDialog);

    public SelectVersionDialog(List<GithubRelease> releases)
    {
        dc = new SelectVersionDialogViewModel(releases);
        DataContext = dc;
        InitializeComponent();
    }

    public new async Task<GithubRelease?> ShowAsync()
    {
        ContentDialogResult result = await ShowAsync(null);
        if (result == ContentDialogResult.Primary)
        {
            return dc.SelectedVersion;
        }
        return null;
    }
}
