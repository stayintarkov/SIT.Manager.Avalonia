using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Controls;
using SIT.Manager.ViewModels.Play;

namespace SIT.Manager.Views.Play;

public partial class ServerSummaryView : ActivatableUserControl
{
    public static readonly StyledProperty<string?> ServerUriProperty =
        AvaloniaProperty.Register<ServerSummaryView, string?>(nameof(ServerUri));
    public string? ServerUri
    {
        get => GetValue(ServerUriProperty);
        set => SetValue(ServerUriProperty, value);
    }

    public ServerSummaryView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<ServerSummaryViewModel>();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (DataContext is ServerSummaryViewModel dataContext)
        {
            //dataContext.ServerUri = (string) Tag;
            dataContext.ServerUri = ServerUri;
        }
    }
}
