using SIT.Manager.Interfaces;
using SIT.Manager.Interfaces.ManagedProcesses;
using System;
using System.Diagnostics;
using System.IO;

namespace SIT.Manager.Services.ManagedProcesses;

public abstract class ManagedProcess(
    IBarNotificationService barNotificationService,
    IManagerConfigService configService) : IManagedProcess
{
    protected readonly IManagerConfigService ConfigService = configService;
    protected readonly IBarNotificationService BarNotificationService = barNotificationService;
    protected Process? ProcessToManage;
    private bool _stopRequest;

    protected abstract string EXECUTABLE_NAME { get; }
    public abstract string ExecutableDirectory { get; }

    public string ExecutableFilePath => !string.IsNullOrEmpty(ExecutableDirectory)
        ? Path.Combine(ExecutableDirectory, EXECUTABLE_NAME)
        : string.Empty;

    public RunningState State { get; private set; } = RunningState.NotRunning;

    public event EventHandler<RunningState>? RunningStateChanged;

    public abstract void ClearCache();

    public abstract void Start(string? arguments);

    public virtual void Stop()
    {
        if (State == RunningState.NotRunning || ProcessToManage == null || ProcessToManage.HasExited) return;

        _stopRequest = true;
        if (ProcessToManage.CloseMainWindow())
        {
            if (ProcessToManage.WaitForExit(TimeSpan.FromSeconds(5))) return;
        }

        ProcessToManage.Kill();
        ProcessToManage.WaitForExit(TimeSpan.FromSeconds(5));
    }

    protected void ExitedEvent(object? sender, EventArgs e)
    {
        RunningState newState = State == RunningState.Running && !_stopRequest
            ? RunningState.StoppedUnexpectedly
            : RunningState.NotRunning;
        _stopRequest = false;
        UpdateRunningState(newState);
    }

    protected void UpdateRunningState(RunningState newState)
    {
        State = newState;
        RunningStateChanged?.Invoke(this, State);
    }
}
