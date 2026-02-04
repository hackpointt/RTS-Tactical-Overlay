using System;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service for executing tactical profile stages with high-resolution timing
/// </summary>
public interface IStageExecutor
{
    /// <summary>
    /// Starts executing the given profile
    /// </summary>
    void Start(TacticalProfile profile);

    /// <summary>
    /// Pauses execution
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes execution
    /// </summary>
    void Resume();

    /// <summary>
    /// Stops execution
    /// </summary>
    void Stop();

    /// <summary>
    /// Whether the executor is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Whether the executor is paused
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Current stage index
    /// </summary>
    int CurrentStageIndex { get; }

    /// <summary>
    /// Elapsed time in current stage (seconds)
    /// </summary>
    double ElapsedTimeInStage { get; }

    /// <summary>
    /// Event raised when the current stage changes
    /// </summary>
    event EventHandler<int>? StageChanged;

    /// <summary>
    /// Event raised when execution completes (non-looping profiles)
    /// </summary>
    event EventHandler? ExecutionCompleted;

    /// <summary>
    /// Event raised periodically (60fps) with progress update
    /// EventArgs: (currentStageIndex, progressInStage 0.0-1.0)
    /// </summary>
    event EventHandler<(int stageIndex, double progress)>? ProgressUpdated;

    /// <summary>
    /// Adjusts the target duration of the currently executing stage at runtime.
    /// Used for drag-and-drop node adjustment during execution.
    /// </summary>
    /// <param name="newDurationSeconds">The new target duration in seconds (minimum 0.1)</param>
    void AdjustCurrentStageDuration(double newDurationSeconds);
}
