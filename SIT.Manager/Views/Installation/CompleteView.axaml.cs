using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels.Installation;

namespace SIT.Manager.Views.Installation;

public partial class CompleteView : UserControl
{
    public CompleteView()
    {
        InitializeComponent();
        this.DataContext = App.Current.Services.GetService<CompleteViewModel>();
    }
}
