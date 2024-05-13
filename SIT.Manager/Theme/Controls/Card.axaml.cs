using Avalonia;
using Avalonia.Controls;

namespace SIT.Manager.Theme.Controls;

public class Card : ContentControl
{
    public static readonly StyledProperty<bool> InsideClippingProperty =
            AvaloniaProperty.Register<Card, bool>(nameof(InsideClipping), true);

    /// <summary>
    /// Get or set the inside border clipping.
    /// </summary>
    public bool InsideClipping
    {
        get => GetValue(InsideClippingProperty);
        set => SetValue(InsideClippingProperty, value);
    }
}
