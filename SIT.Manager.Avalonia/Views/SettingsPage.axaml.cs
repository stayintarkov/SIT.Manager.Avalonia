using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;
using static SIT.Manager.Avalonia.Models.LocalizationConfig;

namespace SIT.Manager.Avalonia.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage() {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<SettingsPageViewModel>();
        }

        private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateTranslationStrings((Languages)languageComboBox.SelectedIndex);
        }
    }
}
