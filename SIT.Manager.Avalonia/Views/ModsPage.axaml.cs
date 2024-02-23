using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class ModsPage : ReactiveUserControl<ModsPageViewModel>
    {
        public ModsPage() {
            this.DataContext = App.Current.Services.GetService<ModsPageViewModel>();
            // ModsPageViewModel's WhenActivated block will also get called.
            this.WhenActivated(disposables => { /* Handle view activation etc. */ });
            InitializeComponent();
        }
    }
}
