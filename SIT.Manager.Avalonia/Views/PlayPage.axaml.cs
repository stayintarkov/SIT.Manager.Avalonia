using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views;

public partial class PlayPage : UserControl
{
    public PlayPage()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<PlayPageViewModel>();
    }
}
