using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage() {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<SettingsPageViewModel>();
        }
    }
}
