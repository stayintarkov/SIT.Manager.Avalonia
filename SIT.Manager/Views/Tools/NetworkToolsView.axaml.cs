using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels.Tools;
using System;
using System.Threading;

namespace SIT.Manager.Views.Tools;

public partial class NetworkToolsView : ActivatableUserControl
{
    private readonly NetworkToolsViewModel _dc;

    public NetworkToolsView()
    {
        InitializeComponent();
        _dc = App.Current.Services.GetService<NetworkToolsViewModel>() ?? throw new Exception("meow");
        this.DataContext = _dc;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _dc.RequestCancellationSource = new CancellationTokenSource();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        _dc.RequestCancellationSource.Cancel();
        _dc.RequestCancellationSource.Dispose();
    }
}
