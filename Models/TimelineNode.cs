using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents the execution state of a timeline node
/// </summary>
public enum NodeExecutionState
{
    /// <summary>
    /// Node has been executed (filled circle)
    /// </summary>
    Executed,

    /// <summary>
    /// Node is currently executing (pulsing filled circle)
    /// </summary>
    Active,

    /// <summary>
    /// Node is pending execution (hollow circle)
    /// </summary>
    Pending
}

/// <summary>
/// Represents a visual node in the timeline UI
/// </summary>
public partial class TimelineNode : ObservableObject
{
    /// <summary>
    /// Reference to the underlying stage
    /// </summary>
    public Stage Stage { get; set; }

    /// <summary>
    /// X position in the timeline (calculated based on cumulative time)
    /// </summary>
    [ObservableProperty]
    private double _xPosition;

    /// <summary>
    /// Width of the node (calculated based on duration)
    /// </summary>
    [ObservableProperty]
    private double _width;

    /// <summary>
    /// Whether this node is currently active
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Execution state of this node (Executed/Active/Pending)
    /// </summary>
    [ObservableProperty]
    private NodeExecutionState _executionState = NodeExecutionState.Pending;

    /// <summary>
    /// Progress percentage for the connecting line animation (0.0 to 1.0)
    /// Used for neon glow effect on the line leading TO this node
    /// </summary>
    [ObservableProperty]
    private double _incomingLineProgress = 0.0;

    /// <summary>
    /// Progress percentage for the node itself (0.0 to 1.0)
    /// Used to animate the node lighting up gradually
    /// </summary>
    [ObservableProperty]
    private double _nodeProgress = 0.0;

    /// <summary>
    /// Color assigned to this node from the palette
    /// </summary>
    [ObservableProperty]
    private Color _nodeColor = Colors.White;

    /// <summary>
    /// Stage index in the profile (for future sliding window implementation)
    /// </summary>
    public int StageIndex { get; set; }

    /// <summary>
    /// Whether this is an end placeholder node (no text, just visual marker)
    /// </summary>
    public bool IsEndPlaceholder { get; set; }

    /// <summary>
    /// Display text for the node
    /// </summary>
    public string DisplayText => IsEndPlaceholder ? "" : $"{Stage?.UnitKey}";

    /// <summary>
    /// Tooltip text showing stage details
    /// </summary>
    public string TooltipText =>
        IsEndPlaceholder ? "End of timeline" :
        $"Unit: {Stage.UnitKey}\n" +
        $"Duration: {Stage.DurationSeconds:F1}s\n" +
        $"{(string.IsNullOrEmpty(Stage.Description) ? "" : Stage.Description)}";

    public TimelineNode(Stage stage, int stageIndex)
    {
        Stage = stage;
        StageIndex = stageIndex;
    }
}
