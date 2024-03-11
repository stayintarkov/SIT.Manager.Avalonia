using System;

namespace SIT.Manager.Avalonia.Models.Installation;

public class InstallStep(Type installView, string header)
{
    public Type InstallationView { get; } = installView;
    public string Header { get; } = header;
}
