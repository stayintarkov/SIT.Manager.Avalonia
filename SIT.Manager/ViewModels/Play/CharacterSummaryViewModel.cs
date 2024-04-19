using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Models.Aki;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Play;

public partial class CharacterSummaryViewModel : ObservableRecipient
{
    [ObservableProperty]
    private AkiMiniProfile _profile;

    [ObservableProperty]
    private double _levelProgressPercentage = 0;

    public IAsyncRelayCommand PlayCommand { get; }

    public CharacterSummaryViewModel(AkiMiniProfile profile)
    {
        Profile = profile;

        double requiredExperience = Profile.NextExperience - Profile.PreviousExperience;
        double currentExperienceProgress = profile.CurrentExperience - Profile.PreviousExperience;
        LevelProgressPercentage = currentExperienceProgress / requiredExperience * 100;

        PlayCommand = new AsyncRelayCommand(Play);
    }

    private async Task Play()
    {
        // TODO
    }
}
