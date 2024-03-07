using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation
{
    public partial class PatchView : UserControl
    {
        public PatchView()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<PatchViewModel>();
        }
    }
}
