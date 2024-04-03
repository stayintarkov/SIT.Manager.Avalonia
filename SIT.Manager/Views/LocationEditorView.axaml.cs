using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.ViewModels;

namespace SIT.Manager.Views;

public partial class LocationEditorView : UserControl
{
    public LocationEditorView()
    {
        this.DataContext = App.Current.Services.GetService<LocationEditorViewModel>();
        InitializeComponent();
    }
}
