using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace SIT.Manager.Avalonia.Converters
{
    internal class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => new SolidColorBrush((Color) (value ?? throw new ArgumentNullException(nameof(value))));

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => ((SolidColorBrush) (value ?? throw new ArgumentNullException(nameof(value)))).Color;
    }
}
