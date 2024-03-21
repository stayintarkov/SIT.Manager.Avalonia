using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views;

public partial class UpdatePage : UserControl
{
    public UpdatePage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<UpdatePageViewModel>();
    }
}
