using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace SIT.Manager.Avalonia.Models
{
    public partial class ConsoleText : ObservableObject
    {
        public SolidColorBrush TextColor { get; set; } = new SolidColorBrush(Colors.White);
        [ObservableProperty]
        private FontFamily _textFont = FontFamily.Default;
        public string Message { get; set; } = string.Empty;
    }
}
