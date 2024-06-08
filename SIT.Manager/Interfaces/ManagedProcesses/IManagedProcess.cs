using System;

namespace SIT.Manager.Interfaces.ManagedProcesses;

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

//This order is important!
public enum RunningState
{
    NotRunning = 0,
    StoppedUnexpectedly = 1,
    Starting = 2,
    Running = 3
}
