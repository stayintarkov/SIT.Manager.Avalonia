using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Views.Dialogs
{
    public partial class SelectVersionDialog : ContentDialog, IStyleable
    {
        private readonly SelectVersionDialogViewModel dc;
        Type IStyleable.StyleKey => typeof(ContentDialog);

        public SelectVersionDialog(List<GithubRelease> releases) {
            dc = new SelectVersionDialogViewModel(releases);
            DataContext = dc;
            InitializeComponent();
        }

        public new async Task<GithubRelease?> ShowAsync() {
            ContentDialogResult result = await ShowAsync(null);
            if (result == ContentDialogResult.Primary) {
                return dc.SelectedVersion;
            }
            return null;
        }
    }
}
