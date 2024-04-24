using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.ViewModels.Play;

public partial class LoginDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = false;

    public LoginDialogViewModel(string username)
    {
        Username = username;
    }
}
