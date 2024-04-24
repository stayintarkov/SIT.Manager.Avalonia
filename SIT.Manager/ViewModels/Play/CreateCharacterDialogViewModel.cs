using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateCharacterDialogViewModel : ObservableValidator
{
    [ObservableProperty]
    private bool _saveLoginDetails = false;

    private string _username = string.Empty;
    private string _password = string.Empty;

    [Required]
    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value, true);
            OnPropertyChanged(nameof(CanCreateCharacter));
        }
    }

    [Required]
    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value, true);
            OnPropertyChanged(nameof(CanCreateCharacter));
        }
    }

    public bool CanCreateCharacter => !HasErrors;
}
