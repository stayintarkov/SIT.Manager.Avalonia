using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Models;

public partial class ConsoleText : ObservableObject
{
    public SolidColorBrush TextColor { get; set; } = new(Colors.White);

    [ObservableProperty] private FontFamily _textFont = FontFamily.Default;

    public string Message { get; set; } = string.Empty;
}
