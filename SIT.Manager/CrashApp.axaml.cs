using Avalonia;
using Avalonia.Markup.Xaml;

namespace SIT.Manager;

public sealed partial class CrashApp : Application
{
    public CrashApp()
    {

    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
