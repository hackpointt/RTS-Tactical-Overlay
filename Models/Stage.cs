using System;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Whether this stage is enabled (disabled stages are skipped during execution)
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional macro to execute instead of simple key press
    /// </summary>
    public Macro? Macro { get; set; }

    /// <summary>
    /// Whether this stage uses a macro (vs simple UnitKey)
    /// </summary>
    [JsonIgnore]
    public bool UseMacro => Macro?.IsValid == true;

    /// <summary>
    /// Display key text for UI (macro display or UnitKey)
    /// </summary>
    [JsonIgnore]
    public string DisplayKey => UseMacro ? Macro!.GetDisplayText() : UnitKey.ToString();

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
        if (DurationSeconds <= 0 || DoubleTapDelayMs < 0)
            return false;

        // Valid if using macro OR using traditional UnitKey
        if (UseMacro)
            return true;

        return UnitKey >= '1' && UnitKey <= '9';
    }
}
