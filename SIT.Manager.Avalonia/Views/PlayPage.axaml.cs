using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class PlayPage : UserControl
    {
        public PlayPage()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<PlayPageViewModel>();
        }
    }
}
