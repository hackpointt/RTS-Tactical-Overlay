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

    // Drag state properties
    [ObservableProperty]
    private bool _isDraggingNode;

    private int _draggedNodeIndex = -1;
    private double _dragStartMouseX;
    private double _dragStartDuration;
    private double _pixelsPerSecondSnapshot;
    private System.Threading.Timer? _saveTimer;
    private string _profileFilePath = "";

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
                _profileFilePath = filePath; // Store for later saves
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

    // Drag-and-drop methods for node interval adjustment
    public void StartNodeDrag(int nodeIndex, double mouseX)
    {
        // Validation
        if (IsExecuting) return; // Don't allow drag during execution
        if (nodeIndex < 0 || nodeIndex >= TimelineNodes.Count) return;
        if (TimelineNodes[nodeIndex].IsEndPlaceholder) return; // Can't drag end placeholder
        if (_profileService.CurrentProfile == null) return;

        // Initialize drag state
        IsDraggingNode = true;
        _draggedNodeIndex = nodeIndex;
        _dragStartMouseX = mouseX;

        // Determine which stage duration to modify
        int stageIndex = (nodeIndex == 0) ? 0 : nodeIndex - 1;
        _dragStartDuration = _profileService.CurrentProfile.Stages[stageIndex].DurationSeconds;

        // Snapshot current pixelsPerSecond to keep scaling consistent during drag
        double availableWidth = System.Windows.SystemParameters.PrimaryScreenWidth * 0.8 * 0.85;
        double totalDuration = _profileService.CurrentProfile.TotalDurationSeconds;
        _pixelsPerSecondSnapshot = totalDuration > 0 ? availableWidth / totalDuration : 50;
        _pixelsPerSecondSnapshot = Math.Max(30, Math.Min(80, _pixelsPerSecondSnapshot));
    }

    public void UpdateNodeDrag(double mouseX)
    {
        if (!IsDraggingNode || _draggedNodeIndex < 0) return;
        if (_profileService.CurrentProfile == null) return;

        // Calculate delta in pixels and convert to time
        double deltaX = mouseX - _dragStartMouseX;
        double newDuration = _dragStartDuration + (deltaX / _pixelsPerSecondSnapshot);

        // Enforce minimum duration (0.5 seconds)
        newDuration = Math.Max(0.5, newDuration);

        // Update the appropriate stage duration
        int stageIndex = (_draggedNodeIndex == 0) ? 0 : _draggedNodeIndex - 1;
        _profileService.CurrentProfile.Stages[stageIndex].DurationSeconds = newDuration;

        // Recalculate positions for affected nodes
        // Start from the node whose line width is changing (stageIndex, not _draggedNodeIndex)
        RecalculateNodePositions(stageIndex);

        // Reset debounce timer for profile save
        ResetSaveTimer();
    }

    public void EndNodeDrag()
    {
        if (!IsDraggingNode) return;

        IsDraggingNode = false;
        _draggedNodeIndex = -1;

        // Timer will trigger save after 3 seconds of inactivity
    }

    private void RecalculateNodePositions(int startNodeIndex)
    {
        if (_profileService.CurrentProfile == null) return;

        // Recalculate cumulative time from the start
        double cumulativeTime = 0;
        for (int i = 0; i < startNodeIndex; i++)
        {
            cumulativeTime += _profileService.CurrentProfile.Stages[i].DurationSeconds;
        }

        // Update positions for nodes from startNodeIndex onwards
        for (int i = startNodeIndex; i < TimelineNodes.Count; i++)
        {
            var node = TimelineNodes[i];

            if (node.IsEndPlaceholder)
            {
                node.XPosition = cumulativeTime * _pixelsPerSecondSnapshot + 50;
                node.Width = 0;
            }
            else
            {
                var stage = _profileService.CurrentProfile.Stages[i];
                node.XPosition = cumulativeTime * _pixelsPerSecondSnapshot;

                // Calculate line width
                double lineWidth = stage.DurationSeconds * _pixelsPerSecondSnapshot;
                if (i == _profileService.CurrentProfile.Stages.Count - 1)
                {
                    lineWidth += 50; // Add gap to reach end placeholder
                }
                node.Width = lineWidth;

                cumulativeTime += stage.DurationSeconds;
            }
        }

        // Update timeline width
        if (TimelineNodes.Count > 0)
        {
            var lastNode = TimelineNodes[TimelineNodes.Count - 1];
            TimelineWidth = lastNode.XPosition + lastNode.Width + 50;
        }
    }

    private void ResetSaveTimer()
    {
        // Cancel existing timer
        _saveTimer?.Dispose();

        // Create new timer that fires after 3 seconds
        _saveTimer = new System.Threading.Timer(
            callback: async _ => await SaveProfileDebounced(),
            state: null,
            dueTime: 3000,
            period: System.Threading.Timeout.Infinite
        );
    }

    private async System.Threading.Tasks.Task SaveProfileDebounced()
    {
        if (_profileService.CurrentProfile == null) return;

        try
        {
            // Determine file path (use last loaded path or default)
            string filePath = string.IsNullOrEmpty(_profileFilePath)
                ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample-profile.json")
                : _profileFilePath;

            await _profileService.SaveProfileAsync(_profileService.CurrentProfile, filePath);

            // Update status on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Profile saved: {_profileService.CurrentProfile.Name}";
            });
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Save error: {ex.Message}";
            });
        }
    }
}
