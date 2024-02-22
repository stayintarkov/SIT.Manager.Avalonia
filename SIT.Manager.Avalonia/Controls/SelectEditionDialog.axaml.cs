using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels;
using System;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia
{
    public partial class SelectEditionDialog : ContentDialog, IStyleable
    {
        Type IStyleable.StyleKey => typeof(ContentDialog);
        private readonly SelectEditionDialogViewModel dc;
        public SelectEditionDialog(TarkovEdition[] editions)
        {
            dc = new SelectEditionDialogViewModel(editions);
            this.DataContext = dc;
            InitializeComponent();
            this.ApplyTemplate();
        }

        public new Task<TarkovEdition> ShowAsync()
        {
            return this.ShowAsync(null).ContinueWith(t => dc.SelectedEdition ?? new TarkovEdition("Edge Of Darkness"));
        }
    }
}
