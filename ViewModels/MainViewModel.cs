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
    private readonly IEdgeGlowService _edgeGlowService;

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

    [ObservableProperty]
    private string _dragDurationText = "";

    [ObservableProperty]
    private double _dragTooltipX;

    // Profile management properties
    [ObservableProperty]
    private ObservableCollection<ProfileIndexEntry> _availableProfiles = new();

    [ObservableProperty]
    private bool _isProfileMenuOpen;

    [ObservableProperty]
    private string _currentProfileAlias = "";

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
        IGlobalHotkeyService hotkeyService,
        IEdgeGlowService edgeGlowService)
    {
        _inputSimulator = inputSimulator;
        _profileService = profileService;
        _stageExecutor = stageExecutor;
        _hotkeyService = hotkeyService;
        _edgeGlowService = edgeGlowService;

        _stageExecutor.StageChanged += OnStageChanged;
        _stageExecutor.ExecutionCompleted += OnExecutionCompleted;
        _stageExecutor.ProgressUpdated += OnProgressUpdated;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Start();

        // Initialize profile system asynchronously
        _ = InitializeProfileSystemAsync();
    }

    private async System.Threading.Tasks.Task InitializeProfileSystemAsync()
    {
        try
        {
            await _profileService.InitializeAsync();
            await RefreshProfileListAsync();

            if (_profileService.CurrentProfile != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentProfileName = _profileService.CurrentProfile.Name;
                    CurrentProfileAlias = _profileService.CurrentProfile.Alias;
                    StatusText = $"Loaded: {_profileService.CurrentProfile.Name}";
                    BuildTimelineNodes(_profileService.CurrentProfile);
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Init error: {ex.Message}";
            });
        }
    }

    private async System.Threading.Tasks.Task RefreshProfileListAsync()
    {
        var profiles = await _profileService.GetAllProfilesAsync();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AvailableProfiles.Clear();
            foreach (var p in profiles)
            {
                AvailableProfiles.Add(p);
            }
        });
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

        // Get edge glow settings (use default if null)
        var glowSettings = _profileService.CurrentProfile.EdgeGlowSettings ?? EdgeGlowSettings.Default;

        // Get initial color for first stage
        var initialColor = NodeColorPalette.GetColorForIndex(0);

        // Start edge glow breathing effect
        _edgeGlowService.StartGlow(initialColor, glowSettings);

        _stageExecutor.Start(_profileService.CurrentProfile);
        IsExecuting = true;
        IsPaused = false;
        StatusText = "Executing...";
    }

    [RelayCommand]
    private void StopExecution()
    {
        _stageExecutor.Stop();
        _edgeGlowService.StopGlow();
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
        _edgeGlowService.PauseGlow();
        IsPaused = true;
        StatusText = "Paused";
    }

    [RelayCommand]
    private void ResumeExecution()
    {
        _stageExecutor.Resume();
        _edgeGlowService.ResumeGlow();
        IsPaused = false;
        StatusText = "Executing...";
    }

    // Profile management commands
    [RelayCommand]
    private async System.Threading.Tasks.Task SwitchProfile(string profileId)
    {
        if (IsExecuting)
        {
            StopExecution();
        }

        var profile = await _profileService.SwitchProfileAsync(profileId);
        if (profile != null)
        {
            CurrentProfileName = profile.Name;
            CurrentProfileAlias = profile.Alias;
            StatusText = $"Switched to: {profile.Name}";
            BuildTimelineNodes(profile);
        }
        IsProfileMenuOpen = false;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CreateNewProfile()
    {
        var profile = await _profileService.CreateProfileAsync("New Profile", "NEW");
        await RefreshProfileListAsync();
        await SwitchProfile(profile.Id);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteProfile(string profileId)
    {
        if (AvailableProfiles.Count <= 1)
        {
            StatusText = "Cannot delete the last profile";
            return;
        }

        await _profileService.DeleteProfileAsync(profileId);
        await RefreshProfileListAsync();

        if (_profileService.CurrentProfile != null)
        {
            CurrentProfileName = _profileService.CurrentProfile.Name;
            CurrentProfileAlias = _profileService.CurrentProfile.Alias;
            BuildTimelineNodes(_profileService.CurrentProfile);
        }
    }

    // Node management commands
    [RelayCommand]
    private void AddNodeAtPosition(double xPosition)
    {
        if (_profileService.CurrentProfile == null) return;
        if (IsExecuting) return;

        // Find insertion index based on x position
        int insertIndex = 0;
        for (int i = 0; i < TimelineNodes.Count; i++)
        {
            if (TimelineNodes[i].IsEndPlaceholder) break;
            if (TimelineNodes[i].XPosition > xPosition) break;
            insertIndex = i + 1;
        }

        // Create new stage with default values
        var newStage = new Stage(2.0, (char)('1' + (insertIndex % 6)), "New Stage");
        _profileService.CurrentProfile.Stages.Insert(insertIndex, newStage);

        // Rebuild timeline
        BuildTimelineNodes(_profileService.CurrentProfile);
        ResetSaveTimer();
    }

    [RelayCommand]
    private void DeleteNode(int stageIndex)
    {
        if (_profileService.CurrentProfile == null) return;
        if (stageIndex < 0 || stageIndex >= _profileService.CurrentProfile.Stages.Count) return;
        if (_profileService.CurrentProfile.Stages.Count <= 1)
        {
            StatusText = "Cannot delete the last node";
            return;
        }

        _profileService.CurrentProfile.Stages.RemoveAt(stageIndex);
        BuildTimelineNodes(_profileService.CurrentProfile);
        ResetSaveTimer();
    }

    [RelayCommand]
    private void ToggleNodeEnabled(int stageIndex)
    {
        if (_profileService.CurrentProfile == null) return;
        if (stageIndex < 0 || stageIndex >= _profileService.CurrentProfile.Stages.Count) return;

        var stage = _profileService.CurrentProfile.Stages[stageIndex];
        stage.IsEnabled = !stage.IsEnabled;

        // Rebuild timeline to reflect the change
        BuildTimelineNodes(_profileService.CurrentProfile);

        ResetSaveTimer();
    }

    // Profile switching via scroll wheel
    public void SwitchToNextProfile()
    {
        if (AvailableProfiles.Count <= 1) return;

        var currentId = _profileService.CurrentProfile?.Id ?? "";
        int currentIndex = AvailableProfiles.ToList().FindIndex(p => p.Id == currentId);
        int nextIndex = (currentIndex + 1) % AvailableProfiles.Count;

        _ = SwitchProfile(AvailableProfiles[nextIndex].Id);
    }

    public void SwitchToPreviousProfile()
    {
        if (AvailableProfiles.Count <= 1) return;

        var currentId = _profileService.CurrentProfile?.Id ?? "";
        int currentIndex = AvailableProfiles.ToList().FindIndex(p => p.Id == currentId);
        int prevIndex = (currentIndex - 1 + AvailableProfiles.Count) % AvailableProfiles.Count;

        _ = SwitchProfile(AvailableProfiles[prevIndex].Id);
    }

    private void OnStageChanged(object? sender, int stageIndex)
    {
        CurrentStageIndex = stageIndex;
        UpdateTimelineActiveState();

        // Update edge glow color to match current node
        if (IsExecuting && _profileService.CurrentProfile != null)
        {
            var color = NodeColorPalette.GetColorForIndex(stageIndex);
            _edgeGlowService.UpdateColor(color);
        }
    }

    private void OnExecutionCompleted(object? sender, EventArgs e)
    {
        _edgeGlowService.StopGlow();
        IsExecuting = false;
        IsPaused = false;
        CurrentStageIndex = -1;
        StatusText = "Completed";
        UpdateTimelineActiveState();
    }

    private void OnProgressUpdated(object? sender, (int stageIndex, double progress) data)
    {
        if (_profileService.CurrentProfile == null) return;
        if (data.stageIndex < 0 || data.stageIndex >= TimelineNodes.Count) return;

        var stages = _profileService.CurrentProfile.Stages;

        // Calculate total duration: current stage + following disabled stages
        double totalDuration = stages[data.stageIndex].DurationSeconds;
        int lastNodeIndex = data.stageIndex;

        for (int i = data.stageIndex + 1; i < stages.Count; i++)
        {
            if (!stages[i].IsEnabled)
            {
                totalDuration += stages[i].DurationSeconds;
                lastNodeIndex = i;
            }
            else
            {
                break;
            }
        }

        // Calculate elapsed time based on progress
        double elapsedTime = data.progress * totalDuration;
        double accumulatedTime = 0;

        // Update each segment's progress
        for (int i = data.stageIndex; i <= lastNodeIndex && i < TimelineNodes.Count; i++)
        {
            double segmentDuration = stages[i].DurationSeconds;
            double segmentStart = accumulatedTime;
            double segmentEnd = accumulatedTime + segmentDuration;

            if (elapsedTime >= segmentEnd)
            {
                // This segment is fully complete
                TimelineNodes[i].IncomingLineProgress = 1.0;
            }
            else if (elapsedTime > segmentStart)
            {
                // This segment is partially complete
                double segmentProgress = (elapsedTime - segmentStart) / segmentDuration;
                TimelineNodes[i].IncomingLineProgress = segmentProgress;
            }
            else
            {
                // This segment hasn't started
                TimelineNodes[i].IncomingLineProgress = 0.0;
            }

            accumulatedTime = segmentEnd;
        }

        // Update the next enabled node's progress
        int nextEnabledIndex = lastNodeIndex + 1;
        if (nextEnabledIndex < TimelineNodes.Count)
        {
            TimelineNodes[nextEnabledIndex].NodeProgress = data.progress;
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
        // Validation - allow drag during execution for runtime adjustment
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

        // Initialize drag tooltip - position at midpoint of the line being adjusted
        DragDurationText = $"{_dragStartDuration:F1} s";
        double lineStartX = TimelineNodes[stageIndex].XPosition + 14; // Node center
        double lineEndX = TimelineNodes[nodeIndex].XPosition + 14;
        DragTooltipX = (lineStartX + lineEndX) / 2;

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

        // If executing and dragging the current stage, notify executor for runtime adjustment
        if (IsExecuting && stageIndex == CurrentStageIndex)
        {
            _stageExecutor.AdjustCurrentStageDuration(newDuration);
        }

        // Recalculate positions for affected nodes FIRST
        RecalculateNodePositions(stageIndex);

        // Update drag tooltip text and position at midpoint of the line (after positions updated)
        DragDurationText = $"{newDuration:F1} s";
        double lineStartX = TimelineNodes[stageIndex].XPosition + 14;
        double lineEndX = TimelineNodes[_draggedNodeIndex].XPosition + 14;
        DragTooltipX = (lineStartX + lineEndX) / 2;

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
            await _profileService.SaveCurrentProfileAsync();

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

    /// <summary>
    /// Refreshes the timeline display after macro edits
    /// </summary>
    public void RefreshTimeline()
    {
        if (_profileService.CurrentProfile == null) return;

        BuildTimelineNodes(_profileService.CurrentProfile);
        ResetSaveTimer();
    }
}
