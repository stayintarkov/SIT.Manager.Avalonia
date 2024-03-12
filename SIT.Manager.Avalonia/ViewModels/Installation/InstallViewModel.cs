using CommunityToolkit.Mvvm.Input;

namespace SIT.Manager.Avalonia.ViewModels.Installation;

public partial class InstallViewModel : InstallationViewModelBase
{
    public InstallViewModel() : base()
    {

    }

    [RelayCommand]
    private void Progress()
    {
        ProgressInstall();
    }
}
