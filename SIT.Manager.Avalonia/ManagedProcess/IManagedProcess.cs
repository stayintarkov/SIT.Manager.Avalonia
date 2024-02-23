using System;

namespace SIT.Manager.Avalonia.ManagedProcess
{
    public interface IManagedProcess
    {
        string ExecutableDirectory { get; }
        string ExecutableFilePath { get; }
        RunningState State { get; }
        event EventHandler<RunningState>? RunningStateChanged;
        /// <summary>
        /// Clear the cache for the process.
        /// </summary>
        void ClearCache();
        void Stop();
        void Start(string? arguments = null);
    }

    public enum RunningState
    {
        NotRunning,
        Running,
        StoppedUnexpectedly
    }
}
