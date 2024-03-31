using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class PlayPage : UserControl
{
    public PlayPage()
    {
        InitializeComponent();

        DataContext = App.Current.Services.GetService<PlayPageViewModel>();
    }
}
