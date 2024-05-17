using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels.Tools;

namespace SIT.Manager.Views.Tools;
public partial class GeneralToolsView : UserControl
{
    public GeneralToolsView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<GeneralToolsViewModel>();
    }
}
