using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels.Installation;

namespace SIT.Manager.Views.Installation;

public partial class SelectView : UserControl
{
    public SelectView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<SelectViewModel>();
    }
}
