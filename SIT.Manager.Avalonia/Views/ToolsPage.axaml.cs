using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class ToolsPage : UserControl
    {
        public ToolsPage() {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<ToolsPageViewModel>();
        }
    }
}
