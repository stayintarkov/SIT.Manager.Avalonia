using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Views.Dialogs
{
    public partial class SelectDowngradePatcherMirrorDialog : ContentDialog
    {
        private readonly SelectDowngradePatcherMirrorDialogViewModel dc;

        protected override Type StyleKeyOverride => typeof(ContentDialog);

        public SelectDowngradePatcherMirrorDialog(Dictionary<string, string> mirrors)
        {
            dc = new SelectDowngradePatcherMirrorDialogViewModel(mirrors);
            DataContext = dc;
            InitializeComponent();
        }

        public new async Task<string?> ShowAsync()
        {
            ContentDialogResult result = await ShowAsync(null);
            if (result == ContentDialogResult.Primary)
            {
                return dc.SelectedMirror?.Value;
            }
            return null;
        }
    }
}
