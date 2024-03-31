using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class UpdatePage : UserControl
{
    public UpdatePage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<UpdatePageViewModel>();
    }
}
