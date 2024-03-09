using System;

namespace SIT.Manager.Avalonia.Models.Installation;

public class InstallStep(int id, Type installView, string header)
{
    public int Id { get; } = id;
    public Type InstallationView { get; } = installView;
    public string Header { get; } = header;
}
