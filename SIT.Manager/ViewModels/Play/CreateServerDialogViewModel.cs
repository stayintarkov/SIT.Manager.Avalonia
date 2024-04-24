using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;

namespace SIT.Manager.ViewModels.Play;

public partial class CreateServerDialogViewModel : ObservableValidator
{
    private string _serverAddress = string.Empty;

    [CustomValidation(typeof(CreateServerDialogViewModel), nameof(ValidateAddress))]
    public string ServerAddress
    {
        get => _serverAddress;
        set
        {
            SetProperty(ref _serverAddress, value, true);
            OnPropertyChanged(nameof(CanCreateServer));
        }
    }

    public Uri ServerUri => ValidateAddress(ServerAddress, new ValidationContext(this)) == ValidationResult.Success ? new(ServerAddress) : new Uri("http://127.0.0.1");

    public bool CanCreateServer => !HasErrors;

    public CreateServerDialogViewModel(string currentServerAddress)
    {
        ServerAddress = currentServerAddress;
    }

    public static ValidationResult? ValidateAddress(string serverAddress, ValidationContext context)
    {
        CreateServerDialogViewModel vm = (CreateServerDialogViewModel) context.ObjectInstance;

        vm.ClearErrors();
        return Uri.IsWellFormedUriString(serverAddress, UriKind.Absolute) ? ValidationResult.Success : new("Uri is not in a valid format.");
    }
}
