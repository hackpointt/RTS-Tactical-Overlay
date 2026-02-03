using System;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents a single stage in the tactical timeline.
/// Each stage has a duration and an associated unit key to simulate.
/// </summary>
public class Stage
{
    /// <summary>
    /// Duration of this stage in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// The unit key to simulate (1-6 for unit groups)
    /// </summary>
    public char UnitKey { get; set; }

    /// <summary>
    /// Optional description for this stage
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to double-tap the key (for selecting unit groups)
    /// Default is true for RTS games
    /// </summary>
    public bool DoubleTap { get; set; } = true;

    /// <summary>
    /// Delay between double-tap key presses in milliseconds
    /// Default is 50ms
    /// </summary>
    public int DoubleTapDelayMs { get; set; } = 50;

    public Stage()
    {
    }

    public Stage(double durationSeconds, char unitKey, string? description = null)
    {
        DurationSeconds = durationSeconds;
        UnitKey = unitKey;
        Description = description;
    }

    /// <summary>
    /// Validates that the stage configuration is valid
    /// </summary>
    public bool IsValid()
    {
        return DurationSeconds > 0 &&
               UnitKey >= '1' && UnitKey <= '6' &&
               DoubleTapDelayMs >= 0;
    }
}
