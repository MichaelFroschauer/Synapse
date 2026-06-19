using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Interfaces;

/// <summary>
/// Allows external code to observe and control a running optimization algorithm.
/// Provides pause/resume/stop capabilities and progress reporting.
/// </summary>
public interface IAlgorithmController
{
    /// <summary>Whether a pause has been requested (algorithm should check this each iteration).</summary>
    bool PauseRequested { get; }
    
    /// <summary>Whether a stop has been requested (algorithm should terminate gracefully).</summary>
    bool StopRequested { get; }
    
    /// <summary>Requests the algorithm to pause. Returns a task that completes when the algorithm acknowledges the pause.</summary>
    Task RequestPauseAsync();
    
    /// <summary>Requests the algorithm to stop.</summary>
    void RequestStop();
    
    /// <summary>Resumes a paused algorithm.</summary>
    void Resume();
    
    /// <summary>Called by the algorithm to signal it has entered the paused state.</summary>
    void SignalPaused();
    
    /// <summary>Called by the algorithm to signal it has resumed execution.</summary>
    void SignalResumed();
    
    /// <summary>Called by the algorithm to report iteration progress.</summary>
    void SignalProgress(ProgressEventArgs progress);
    
    /// <summary>Raised when the algorithm has paused.</summary>
    event Action? Paused;
    
    /// <summary>Raised when the algorithm has resumed.</summary>
    event Action? Resumed;
    
    /// <summary>Raised on each progress report from the algorithm.</summary>
    event Action<ProgressEventArgs>? OnProgress;
}
