using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class LocationEditorView : UserControl
    {
        public LocationEditorView() {
            this.DataContext = App.Current.Services.GetService<LocationEditorViewModel>();
            InitializeComponent();
        }
    }
}
