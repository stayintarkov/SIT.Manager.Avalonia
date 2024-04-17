using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels.Play;

namespace SIT.Manager.Views.Play;

public partial class CharacterSelectionView : ActivatableUserControl
{
    public CharacterSelectionView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<CharacterSelectionViewModel>();
    }
}
