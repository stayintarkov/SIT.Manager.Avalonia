using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Services;
using SIT.Manager.Services.Install;
using SIT.Manager.ViewModels;
using SIT.Manager.ViewModels.Installation;
using SIT.Manager.Views;
using System;
using System.Net.Http;

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
