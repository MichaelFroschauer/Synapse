using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

/// <summary>
/// Default implementation of <see cref="IAlgorithmController"/>.
/// Provides thread-safe pause/resume/stop control and progress forwarding.
/// </summary>
public class AlgorithmController : IAlgorithmController
{
    public bool PauseRequested { get; private set; }
    public bool StopRequested { get; private set; }

    private readonly Lock _pauseLock = new();
    private TaskCompletionSource<bool>? _pausedTcs;
    
    public event Action? Paused;
    public event Action? Resumed;
    public event Action<ProgressEventArgs>? OnProgress;
    
    /// <inheritdoc />
    public Task RequestPauseAsync()
    {
        lock (_pauseLock)
        {
            if (PauseRequested)
                return _pausedTcs?.Task ?? Task.CompletedTask;

            PauseRequested = true;
            _pausedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _pausedTcs.Task;
        }
    }
    
    /// <inheritdoc />
    public void RequestStop() => StopRequested = true;

    /// <inheritdoc />
    public void Resume()
    {
        lock (_pauseLock)
        {
            PauseRequested = false;
            _pausedTcs = null;
        }

        Resumed?.Invoke();
    }

    /// <inheritdoc />
    public void SignalPaused()
    {
        lock (_pauseLock)
        {
            _pausedTcs?.TrySetResult(true);
        }

        try { Paused?.Invoke(); }
        catch { /* subscriber errors must not crash the algorithm */ }
    }

    /// <inheritdoc />
    public void SignalResumed()
    {
        try { Resumed?.Invoke(); }
        catch { /* subscriber errors must not crash the algorithm */ }
    }
    
    /// <inheritdoc />
    public void SignalProgress(ProgressEventArgs progress)
    {
        try { OnProgress?.Invoke(progress); }
        catch { /* subscriber errors must not crash the algorithm */ }
    }
}