using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Extentions;
using SIT.Manager.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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

    [ObservableProperty]
    private TarkovEdition _selectedEdition;

    public ObservableCollection<TarkovEdition> Editions { get; } = [];

    public bool CanCreateCharacter => !HasErrors;

    public CreateCharacterDialogViewModel(string username, string password, bool rememberLogin, TarkovEdition[] editions)
    {
        Username = username;
        Password = password;
        SaveLoginDetails = rememberLogin;

        Editions.AddRange(editions);
        SelectedEdition = Editions.First();
    }
}
