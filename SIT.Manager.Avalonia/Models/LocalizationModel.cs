using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Avalonia.Models
{
    public partial class LocalizationModel : ObservableObject
    {
        [ObservableProperty]
        public string _shortNameLanguage = string.Empty;
        [ObservableProperty]
        public string _fullNameLanguage = string.Empty;
    }
}