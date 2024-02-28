using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIT.Manager.Avalonia.ManagedProcess
{
    public interface IAkiServerService : IManagedProcess
    {
        event EventHandler<DataReceivedEventArgs>? OutputDataReceived;
        event EventHandler? ServerStarted;

        bool IsStarted { get; }
        int ServerLineLimit { get; }

        /// <summary>
        /// Gets all the output which would have gone to the OutputDataReceived event
        /// if there are no event listeners attached.
        /// </summary>
        /// <returns>A list of all the strings which weren't sent to the OutputDataReceived as there was no listeners</returns>
        List<string> GetCachedServerOutput();
        bool IsUnhandledInstanceRunning();
    }
}
