using System;
using System.Collections.Generic;
using System.Linq;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents a complete tactical profile with multiple stages
/// </summary>
public class TacticalProfile
{
    /// <summary>
    /// Unique identifier for the profile
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Profile name
    /// </summary>
    public string Name { get; set; } = "Unnamed Profile";

    /// <summary>
    /// Short alias for UI display (max 5 characters)
    /// </summary>
    public string Alias { get; set; } = "";

    /// <summary>
    /// Profile description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// List of stages in this profile
    /// </summary>
    public List<Stage> Stages { get; set; } = new();

    /// <summary>
    /// Whether to loop the profile when it reaches the end
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Edge glow breathing effect settings
    /// Null means use default settings
    /// </summary>
    public EdgeGlowSettings? EdgeGlowSettings { get; set; }

    /// <summary>
    /// Gets the total duration of all stages in seconds
    /// </summary>
    public double TotalDurationSeconds => Stages.Sum(s => s.DurationSeconds);

    /// <summary>
    /// Validates that the profile is valid
    /// Also clamps EdgeGlowSettings to safe values
    /// </summary>
    public bool IsValid()
    {
        // Clamp edge glow settings if present
        if (EdgeGlowSettings != null)
        {
            EdgeGlowSettings.ClampValues();
        }

        return !string.IsNullOrWhiteSpace(Name) &&
               Stages.Count > 0 &&
               Stages.All(s => s.IsValid());
    }
}
