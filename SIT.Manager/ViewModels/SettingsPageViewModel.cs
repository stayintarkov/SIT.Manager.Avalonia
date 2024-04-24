using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SIT.Manager.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _managerVersionString;

    public SettingsPageViewModel()
    {
        ManagerVersionString = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A";
    }
}
