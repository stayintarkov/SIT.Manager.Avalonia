using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels.Installation;

namespace SIT.Manager.Avalonia.Views.Installation
{
    public partial class ConfigureView : ReactiveUserControl<ConfigureViewModel>
    {
        public ConfigureView()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<ConfigureViewModel>();
            this.WhenActivated(disposables => { /* Handle view activation etc. */ });
        }
    }
}
