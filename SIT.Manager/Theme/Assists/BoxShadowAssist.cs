using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SIT.Manager.Theme.Assists;

public static class BoxShadowAssist
{
    public static readonly AvaloniaProperty<bool> InsetProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("Inset", typeof(BoxShadowAssist), false);
    public static readonly AvaloniaProperty<double> BlurProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, double>("Blur", typeof(BoxShadowAssist), 8);
    public static readonly AvaloniaProperty<double> OffsetXProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, double>("OffsetX", typeof(BoxShadowAssist), 1.5);
    public static readonly AvaloniaProperty<double> OffsetYProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, double>("OffsetY", typeof(BoxShadowAssist), 1.5);
    public static readonly AvaloniaProperty<Color> ColorProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, Color>("Color", typeof(BoxShadowAssist), new Color(76, 0, 0, 0));

    static BoxShadowAssist()
    {
        InsetProperty.Changed.Subscribe(InsetPropertyCallback);
        BlurProperty.Changed.Subscribe(BlurPropertyCallback);
        OffsetXProperty.Changed.Subscribe(OffsetXPropertyCallback);
        OffsetYProperty.Changed.Subscribe(OffsetYPropertyCallback);
        ColorProperty.Changed.Subscribe(ColorPropertyCallback);
    }



    private static BoxShadows UpdateBoxShadow(bool inset, double blur, double offsetX, double offsetY, Color color)
    {
        return new BoxShadows(new()
        {
            IsInset = inset,
            Blur = blur,
            OffsetX = offsetX,
            OffsetY = offsetY,
            Color = color
        });
    }

    private static void InsetPropertyCallback(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (args.Sender is Border border)
        {
            border.BoxShadow = UpdateBoxShadow(args.NewValue.Value, GetBlur(args.Sender), GetOffsetX(args.Sender), GetOffsetY(args.Sender), GetColor(args.Sender));
        }
    }

    private static void BlurPropertyCallback(AvaloniaPropertyChangedEventArgs<double> args)
    {
        if (args.Sender is Border border)
        {
            border.BoxShadow = UpdateBoxShadow(GetInset(args.Sender), args.NewValue.Value, GetOffsetX(args.Sender), GetOffsetY(args.Sender), GetColor(args.Sender));
        }
    }

    private static void OffsetXPropertyCallback(AvaloniaPropertyChangedEventArgs<double> args)
    {
        if (args.Sender is Border border)
        {
            border.BoxShadow = UpdateBoxShadow(GetInset(args.Sender), GetBlur(args.Sender), args.NewValue.Value, GetOffsetY(args.Sender), GetColor(args.Sender));
        }
    }

    private static void OffsetYPropertyCallback(AvaloniaPropertyChangedEventArgs<double> args)
    {
        if (args.Sender is Border border)
        {
            border.BoxShadow = UpdateBoxShadow(GetInset(args.Sender), GetBlur(args.Sender), GetOffsetX(args.Sender), args.NewValue.Value, GetColor(args.Sender));
        }
    }

    private static void ColorPropertyCallback(AvaloniaPropertyChangedEventArgs<Color> args)
    {
        if (args.Sender is Border border)
        {
            border.BoxShadow = UpdateBoxShadow(GetInset(args.Sender), GetBlur(args.Sender), GetOffsetX(args.Sender), GetOffsetY(args.Sender), args.NewValue.Value);
        }
    }

    public static void SetInset(AvaloniaObject element, bool value) => element.SetValue(InsetProperty, value);
    public static bool GetInset(AvaloniaObject element) => element.GetValue<bool>(InsetProperty);

    public static void SeBlur(AvaloniaObject element, double value) => element.SetValue(BlurProperty, value);
    public static double GetBlur(AvaloniaObject element) => element.GetValue<double>(BlurProperty);

    public static void SetOffsetX(AvaloniaObject element, double value) => element.SetValue(OffsetXProperty, value);
    public static double GetOffsetX(AvaloniaObject element) => element.GetValue<double>(OffsetXProperty);

    public static void SetOffsetY(AvaloniaObject element, double value) => element.SetValue(OffsetYProperty, value);
    public static double GetOffsetY(AvaloniaObject element) => element.GetValue<double>(OffsetYProperty);

    public static void SetColor(AvaloniaObject element, Color value) => element.SetValue(ColorProperty, value);
    public static Color GetColor(AvaloniaObject element) => element.GetValue<Color>(ColorProperty);
}
