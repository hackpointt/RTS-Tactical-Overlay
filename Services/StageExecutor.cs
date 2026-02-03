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
                _stageStopwatch.Restart();
                _pausedElapsedTime = 0;

                StageChanged?.Invoke(this, _currentStageIndex);
                ExecuteStageKeyPress(stage);
                await WaitForStageDuration(stage.DurationSeconds, cancellationToken);

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
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing key press: {ex.Message}");
        }
    }

    private async Task WaitForStageDuration(double durationSeconds, CancellationToken cancellationToken)
    {
        var targetTime = TimeSpan.FromSeconds(durationSeconds);
        var frameInterval = TimeSpan.FromMilliseconds(16.67); // 60fps

        while (_stageStopwatch.Elapsed < targetTime && !cancellationToken.IsCancellationRequested)
        {
            while (_isPaused && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Calculate progress (0.0 to 1.0)
            double progress = Math.Min(1.0, _stageStopwatch.Elapsed.TotalSeconds / durationSeconds);
            ProgressUpdated?.Invoke(this, (_currentStageIndex, progress));

            var remaining = targetTime - _stageStopwatch.Elapsed;
            var delayTime = remaining < frameInterval ? remaining : frameInterval;

            if (delayTime > TimeSpan.Zero)
            {
                await Task.Delay(delayTime, cancellationToken);
            }
        }
    }
}
