using Avalonia.Controls;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;
public partial class CrashWindow : Window
{
    public CrashWindow()
    {
        InitializeComponent();
        this.DataContext = new CrashWindowViewModel();
    }
}
