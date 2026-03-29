using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Executes tactical profile stages with high-resolution timing
/// </summary>
public class StageExecutor : IStageExecutor
{
    private readonly INativeInputSimulator _inputSimulator;
    private TacticalProfile? _currentProfile;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _executionTask;
    private readonly Stopwatch _stageStopwatch;
    private int _currentStageIndex;
    private bool _isPaused;
    private double _pausedElapsedTime;

    // Runtime duration adjustment support
    private double _currentTargetDuration;
    private readonly object _durationLock = new object();

    public bool IsRunning { get; private set; }
    public bool IsPaused => _isPaused;
    public int CurrentStageIndex => _currentStageIndex;
    public double ElapsedTimeInStage => _isPaused ? _pausedElapsedTime : _stageStopwatch.Elapsed.TotalSeconds;

    public event EventHandler<int>? StageChanged;
    public event EventHandler? ExecutionCompleted;
    public event EventHandler<(int stageIndex, double progress)>? ProgressUpdated;

    public StageExecutor(INativeInputSimulator inputSimulator)
    {
        _inputSimulator = inputSimulator;
        _stageStopwatch = new Stopwatch();
    }

    public void Start(TacticalProfile profile)
    {
        if (IsRunning)
        {
            Stop();
        }

        if (!profile.IsValid())
        {
            throw new ArgumentException("Invalid profile", nameof(profile));
        }

        _currentProfile = profile;
        _currentStageIndex = 0;
        _isPaused = false;
        _pausedElapsedTime = 0;
        IsRunning = true;

        _cancellationTokenSource = new CancellationTokenSource();
        _executionTask = Task.Run(() => ExecutionLoop(_cancellationTokenSource.Token));
    }

    public void Pause()
    {
        if (!IsRunning || _isPaused)
        {
            return;
        }

        _isPaused = true;
        _pausedElapsedTime = _stageStopwatch.Elapsed.TotalSeconds;
        _stageStopwatch.Stop();
    }

    public void Resume()
    {
        if (!IsRunning || !_isPaused)
        {
            return;
        }

        _isPaused = false;
        _stageStopwatch.Start();
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _isPaused = false;
        _cancellationTokenSource?.Cancel();
        _executionTask?.Wait(TimeSpan.FromSeconds(1));
        _stageStopwatch.Stop();
        _stageStopwatch.Reset();
    }

    private async Task ExecutionLoop(CancellationToken cancellationToken)
    {
        if (_currentProfile == null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (_isPaused && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var stage = _currentProfile.Stages[_currentStageIndex];

                // Skip disabled stages (handled by accumulating their duration)
                if (!stage.IsEnabled)
                {
                    _currentStageIndex++;
                    if (_currentStageIndex >= _currentProfile.Stages.Count)
                    {
                        if (_currentProfile.Loop)
                        {
                            _currentStageIndex = 0;
                        }
                        else
                        {
                            IsRunning = false;
                            ExecutionCompleted?.Invoke(this, EventArgs.Empty);
                            break;
                        }
                    }
                    continue;
                }

                _stageStopwatch.Restart();
                _pausedElapsedTime = 0;

                StageChanged?.Invoke(this, _currentStageIndex);
                ExecuteStageKeyPress(stage);

                // Calculate total duration: current stage + all following disabled stages
                double totalDuration = stage.DurationSeconds;
                int nextIndex = _currentStageIndex + 1;
                while (nextIndex < _currentProfile.Stages.Count &&
                       !_currentProfile.Stages[nextIndex].IsEnabled)
                {
                    totalDuration += _currentProfile.Stages[nextIndex].DurationSeconds;
                    nextIndex++;
                }

                await WaitForStageDuration(totalDuration, cancellationToken);

                _currentStageIndex++;

                if (_currentStageIndex >= _currentProfile.Stages.Count)
                {
                    if (_currentProfile.Loop)
                    {
                        _currentStageIndex = 0;
                    }
                    else
                    {
                        IsRunning = false;
                        ExecutionCompleted?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in execution loop: {ex.Message}");
            IsRunning = false;
        }
    }

    private void ExecuteStageKeyPress(Stage stage)
    {
        try
        {
            if (stage.UseMacro)
            {
                ExecuteMacro(stage.Macro!);
            }
            else
            {
                // Original logic: backward compatible
                if (stage.DoubleTap)
                {
                    _inputSimulator.SendKeyPress(stage.UnitKey);
                    Thread.Sleep(stage.DoubleTapDelayMs);
                    _inputSimulator.SendKeyPress(stage.UnitKey);
                }
                else
                {
                    _inputSimulator.SendKeyPress(stage.UnitKey);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing key press: {ex.Message}");
        }
    }

    private void ExecuteMacro(Macro macro)
    {
        foreach (var action in macro.Actions)
        {
            switch (action.Type)
            {
                case MacroActionType.KeyPress:
                    ExecuteKeyPress(action);
                    break;
                case MacroActionType.KeyDown:
                    ExecuteKeyDown(action);
                    break;
                case MacroActionType.KeyUp:
                    ExecuteKeyUp(action);
                    break;
                case MacroActionType.Delay:
                    Thread.Sleep(action.DelayMs);
                    break;
            }
        }
    }

    private void ExecuteKeyPress(MacroAction action)
    {
        if (action.Modifiers?.Count > 0)
        {
            // Press modifiers
            foreach (var mod in action.Modifiers)
                _inputSimulator.SendKeyDown(mod);

            // Press key
            if (action.Key.HasValue)
                _inputSimulator.SendKeyPress(action.Key.Value);
            else if (action.VirtualKeyCode.HasValue)
                _inputSimulator.SendKeyPress(action.VirtualKeyCode.Value);

            // Release modifiers
            foreach (var mod in action.Modifiers)
                _inputSimulator.SendKeyUp(mod);
        }
        else
        {
            if (action.Key.HasValue)
                _inputSimulator.SendKeyPress(action.Key.Value);
            else if (action.VirtualKeyCode.HasValue)
                _inputSimulator.SendKeyPress(action.VirtualKeyCode.Value);
        }

        if (action.DelayMs > 0 && action.Type == MacroActionType.KeyPress)
            Thread.Sleep(action.DelayMs);
    }

    private void ExecuteKeyDown(MacroAction action)
    {
        if (action.Key.HasValue)
            _inputSimulator.SendKeyDown((ushort)char.ToUpper(action.Key.Value));
        else if (action.VirtualKeyCode.HasValue)
            _inputSimulator.SendKeyDown(action.VirtualKeyCode.Value);
    }

    private void ExecuteKeyUp(MacroAction action)
    {
        if (action.Key.HasValue)
            _inputSimulator.SendKeyUp((ushort)char.ToUpper(action.Key.Value));
        else if (action.VirtualKeyCode.HasValue)
            _inputSimulator.SendKeyUp(action.VirtualKeyCode.Value);
    }

    private async Task WaitForStageDuration(double durationSeconds, CancellationToken cancellationToken)
    {
        // Initialize the target duration (can be adjusted at runtime via AdjustCurrentStageDuration)
        lock (_durationLock)
        {
            _currentTargetDuration = durationSeconds;
        }

        var frameInterval = TimeSpan.FromMilliseconds(16.67); // 60fps

        while (!cancellationToken.IsCancellationRequested)
        {
            while (_isPaused && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Read current target duration (may have been adjusted by drag)
            double currentTarget;
            lock (_durationLock)
            {
                currentTarget = _currentTargetDuration;
            }

            var targetTime = TimeSpan.FromSeconds(currentTarget);

            // Check if we've reached the target
            if (_stageStopwatch.Elapsed >= targetTime)
            {
                break;
            }

            // Calculate progress (0.0 to 1.0)
            double progress = Math.Min(1.0, _stageStopwatch.Elapsed.TotalSeconds / currentTarget);
            ProgressUpdated?.Invoke(this, (_currentStageIndex, progress));

            var remaining = targetTime - _stageStopwatch.Elapsed;
            var delayTime = remaining < frameInterval ? remaining : frameInterval;

            if (delayTime > TimeSpan.Zero)
            {
                await Task.Delay(delayTime, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Adjusts the target duration of the currently executing stage at runtime.
    /// </summary>
    public void AdjustCurrentStageDuration(double newDurationSeconds)
    {
        if (!IsRunning) return;

        lock (_durationLock)
        {
            _currentTargetDuration = Math.Max(0.1, newDurationSeconds);
        }
    }
}
