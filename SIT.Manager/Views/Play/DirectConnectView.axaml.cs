using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels.Play;

namespace SIT.Manager.Views.Play;
public partial class DirectConnectView : UserControl
{
    public DirectConnectView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<DirectConnectViewModel>();
    }
}
