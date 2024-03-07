using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation
{
    public partial class DownloadView : UserControl
    {
        public DownloadView()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<DownloadViewModel>();
        }
    }
}
