using System;
using System.Diagnostics;

namespace SIT.Manager.Avalonia.ManagedProcess
{
    public interface IAkiServerService : IManagedProcess
    {
        event EventHandler<DataReceivedEventArgs>? OutputDataReceived;
        event EventHandler? ServerStarted;
        public bool IsStarted { get; }
        bool IsUnhandledInstanceRunning();
    }
}
