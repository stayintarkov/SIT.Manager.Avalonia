using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Interfaces.ManagedProcesses;
using SIT.Manager.Models.Aki;
using SIT.Manager.Views.Play;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSummaryViewModel : ObservableRecipient
{
    private readonly ITarkovClientService _tarkovClientService;

    private readonly AkiServer _connectedServer;

    [ObservableProperty]
    private AkiMiniProfile _profile;

    [ObservableProperty]
    private double _levelProgressPercentage = 0;

    public IAsyncRelayCommand PlayCommand { get; }

    public CharacterSummaryViewModel(AkiServer server, AkiMiniProfile profile)
    {
        _connectedServer = server;
        Profile = profile;

        double requiredExperience = Profile.NextExperience - Profile.PreviousExperience;
        double currentExperienceProgress = profile.CurrentExperience - Profile.PreviousExperience;
        LevelProgressPercentage = currentExperienceProgress / requiredExperience * 100;

        PlayCommand = new AsyncRelayCommand(Play);
    }

    private async Task Play()
    {
        // TODO check if character already exists as saved login

        LoginDialogView dialog = new(Profile.Username);
        (ContentDialogResult result, string password, bool rememberLogin) = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        // TODO make persitent
        AkiCharacter character = new(_connectedServer, Profile.Username, password);

        await _tarkovClientService.ConnectToServer(character);
    }
}
