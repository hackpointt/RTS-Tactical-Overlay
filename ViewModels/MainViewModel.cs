using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Services;

namespace RTS_Tactical_Overlay.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INativeInputSimulator _inputSimulator;
    private readonly IProfileService _profileService;
    private readonly IStageExecutor _stageExecutor;
    private readonly IGlobalHotkeyService _hotkeyService;

    [ObservableProperty]
    private string _statusText = "Press F5 to load sample profile";

    [ObservableProperty]
    private string _currentProfileName = "No Profile Loaded - Press F5";

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private int _currentStageIndex = -1;

    [ObservableProperty]
    private double _elapsedTimeInStage;

    [ObservableProperty]
    private ObservableCollection<TimelineNode> _timelineNodes = new();

    [ObservableProperty]
    private double _timelineWidth = 100; // Default minimum width

    public MainViewModel(
        INativeInputSimulator inputSimulator,
        IProfileService profileService,
        IStageExecutor stageExecutor,
        IGlobalHotkeyService hotkeyService)
    {
        _inputSimulator = inputSimulator;
        _profileService = profileService;
        _stageExecutor = stageExecutor;
        _hotkeyService = hotkeyService;

        _stageExecutor.StageChanged += OnStageChanged;
        _stageExecutor.ExecutionCompleted += OnExecutionCompleted;
        _stageExecutor.ProgressUpdated += OnProgressUpdated;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Start();
    }

    [RelayCommand]
    private async void LoadProfile(string filePath)
    {
        try
        {
            var profile = await _profileService.LoadProfileAsync(filePath);
            if (profile != null)
            {
                CurrentProfileName = profile.Name;
                StatusText = $"Loaded: {profile.Name}";
                BuildTimelineNodes(profile);
            }
            else
            {
                StatusText = "Failed to load profile";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartExecution()
    {
        if (_profileService.CurrentProfile == null)
        {
            StatusText = "No profile loaded";
            return;
        }

        _stageExecutor.Start(_profileService.CurrentProfile);
        IsExecuting = true;
        IsPaused = false;
        StatusText = "Executing...";
    }

    [RelayCommand]
    private void StopExecution()
    {
        _stageExecutor.Stop();
        IsExecuting = false;
        IsPaused = false;
        CurrentStageIndex = -1;
        StatusText = "Stopped";
        UpdateTimelineActiveState();
    }

    [RelayCommand]
    private void PauseExecution()
    {
        _stageExecutor.Pause();
        IsPaused = true;
        StatusText = "Paused";
    }

    [RelayCommand]
    private void ResumeExecution()
    {
        _stageExecutor.Resume();
        IsPaused = false;
        StatusText = "Executing...";
    }

    private void OnStageChanged(object? sender, int stageIndex)
    {
        CurrentStageIndex = stageIndex;
        UpdateTimelineActiveState();
    }

    private void OnExecutionCompleted(object? sender, EventArgs e)
    {
        IsExecuting = false;
        IsPaused = false;
        CurrentStageIndex = -1;
        StatusText = "Completed";
        UpdateTimelineActiveState();
    }

    private void OnProgressUpdated(object? sender, (int stageIndex, double progress) data)
    {
        // Update the current stage's outgoing line progress (60fps smooth animation)
        // The line is drawn from current node, so we update current node's IncomingLineProgress
        if (data.stageIndex >= 0 && data.stageIndex < TimelineNodes.Count)
        {
            TimelineNodes[data.stageIndex].IncomingLineProgress = data.progress;

            // Also update the next node's progress (node lights up as line reaches it)
            int nextNodeIndex = data.stageIndex + 1;
            if (nextNodeIndex < TimelineNodes.Count)
            {
                TimelineNodes[nextNodeIndex].NodeProgress = data.progress;
            }
        }
    }

    private void OnHotkeyPressed(object? sender, string action)
    {
        Console.WriteLine($"[MainViewModel] Hotkey pressed: {action}");
        switch (action)
        {
            case "LoadProfile":
                LoadSampleProfile();
                break;
            case "StartExecution":
                if (!IsExecuting) StartExecution();
                break;
            case "PauseResume":
                if (IsExecuting && !IsPaused) PauseExecution();
                else if (IsExecuting && IsPaused) ResumeExecution();
                break;
            case "StopExecution":
                if (IsExecuting) StopExecution();
                break;
        }
    }

    private void LoadSampleProfile()
    {
        var samplePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample-profile.json");
        if (System.IO.File.Exists(samplePath))
        {
            LoadProfileCommand.Execute(samplePath);
        }
    }

    private void BuildTimelineNodes(TacticalProfile profile)
    {
        TimelineNodes.Clear();
        double cumulativeTime = 0;

        // Calculate pixels per second based on available space
        // Aim to use about 70% of available width for better spacing
        double availableWidth = System.Windows.SystemParameters.PrimaryScreenWidth * 0.8 * 0.85; // 85% of bar width
        double totalDuration = profile.TotalDurationSeconds;
        double pixelsPerSecond = totalDuration > 0 ? availableWidth / totalDuration : 50;

        // Clamp to reasonable range (min 30, max 80 pixels per second)
        pixelsPerSecond = Math.Max(30, Math.Min(80, pixelsPerSecond));

        for (int i = 0; i < profile.Stages.Count; i++)
        {
            var stage = profile.Stages[i];

            // For the last stage, add 50px to connect to end placeholder
            double lineWidth = stage.DurationSeconds * pixelsPerSecond;
            if (i == profile.Stages.Count - 1)
            {
                lineWidth += 50; // Add gap to reach end placeholder
            }

            var node = new TimelineNode(stage, i)
            {
                XPosition = cumulativeTime * pixelsPerSecond,
                Width = lineWidth,
                ExecutionState = NodeExecutionState.Pending,
                IncomingLineProgress = 0.0,
                NodeColor = NodeColorPalette.GetColorForIndex(i) // Assign color from palette
            };
            TimelineNodes.Add(node);
            cumulativeTime += stage.DurationSeconds;
        }

        // Add end placeholder node (empty circle at the end with 50px gap)
        if (profile.Stages.Count > 0)
        {
            var endNode = new TimelineNode(null!, -1)
            {
                XPosition = cumulativeTime * pixelsPerSecond + 50,
                Width = 0, // No line from end placeholder
                ExecutionState = NodeExecutionState.Pending,
                IncomingLineProgress = 0.0,
                IsEndPlaceholder = true
            };
            TimelineNodes.Add(endNode);
        }

        // Calculate total timeline width (last node position + last node width + padding)
        if (TimelineNodes.Count > 0)
        {
            var lastNode = TimelineNodes[TimelineNodes.Count - 1];
            TimelineWidth = lastNode.XPosition + lastNode.Width + 50; // Add 50px padding
        }
        else
        {
            TimelineWidth = 100; // Minimum width when no nodes
        }
    }

    private void UpdateTimelineActiveState()
    {
        for (int i = 0; i < TimelineNodes.Count; i++)
        {
            var node = TimelineNodes[i];
            node.IsActive = (i == CurrentStageIndex);

            // Special handling for end placeholder
            if (node.IsEndPlaceholder)
            {
                // End placeholder should light up ONLY when execution is complete
                // Reset when starting a new execution
                if (!IsExecuting && CurrentStageIndex == -1)
                {
                    // Check if we just completed (all nodes were executed)
                    bool allCompleted = TimelineNodes.Take(TimelineNodes.Count - 1)
                        .All(n => n.ExecutionState == NodeExecutionState.Executed);

                    if (allCompleted)
                    {
                        node.ExecutionState = NodeExecutionState.Executed;
                        node.IncomingLineProgress = 1.0;
                        node.NodeProgress = 1.0;
                    }
                    else
                    {
                        // Stopped or reset - turn off
                        node.ExecutionState = NodeExecutionState.Pending;
                        node.IncomingLineProgress = 0.0;
                        node.NodeProgress = 0.0;
                    }
                }
                else
                {
                    // During execution - stays pending until last stage
                    node.ExecutionState = NodeExecutionState.Pending;
                    node.IncomingLineProgress = 0.0;
                    node.NodeProgress = 0.0;
                }
                continue;
            }

            // Update execution state based on current stage
            if (i < CurrentStageIndex)
            {
                // Already executed
                node.ExecutionState = NodeExecutionState.Executed;
                node.IncomingLineProgress = 1.0; // Fully lit neon line
                node.NodeProgress = 1.0; // Node fully lit
            }
            else if (i == CurrentStageIndex)
            {
                // Currently executing
                node.ExecutionState = NodeExecutionState.Active;
                node.NodeProgress = 1.0; // Current node is fully lit
                // Don't set IncomingLineProgress here - let OnProgressUpdated handle it
                // This prevents the flash when stage changes
            }
            else
            {
                // Pending execution
                node.ExecutionState = NodeExecutionState.Pending;
                node.IncomingLineProgress = 0.0; // Line not lit yet
                node.NodeProgress = 0.0; // Node not lit yet
            }
        }
    }
}
