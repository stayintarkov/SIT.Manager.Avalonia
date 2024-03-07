using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation
{
    public partial class ConfigureView : UserControl
    {
        public ConfigureView()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<ConfigureViewModel>();
        }
    }
}
