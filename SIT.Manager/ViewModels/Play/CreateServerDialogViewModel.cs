using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.ViewModels.Play;

public class CreateServerDialogViewModel : ObservableObject
{
    public string ServerCreationData { get; }

    public CreateServerDialogViewModel()
    {
        ServerCreationData = "1";
    }
}
