using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SIT.Manager.Models.Config;

public class ManagerConfig
{
    public LauncherConfig LauncherSettings { get; init; } = new();
    public LinuxConfig LinuxSettings { get; init; } = new();
    public SITConfig SITSettings { get; init; } = new();
    public AkiConfig AkiSettings { get; init; } = new();
}
