using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Models;

public partial class DiagnosticsOptions : ObservableObject
{
    [ObservableProperty]
    private bool _includeClientLog = true;
    [ObservableProperty]
    public bool _includeServerLog = true;
    [ObservableProperty]
    public bool _includeDiagnosticLog = true;
    [ObservableProperty]
    public bool _includeHttpJson = true;
    [ObservableProperty]
    public bool _includeManagerLog = true;
}
