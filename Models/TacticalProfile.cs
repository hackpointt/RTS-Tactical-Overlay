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
    /// Profile name
    /// </summary>
    public string Name { get; set; } = "Unnamed Profile";

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
    /// Gets the total duration of all stages in seconds
    /// </summary>
    public double TotalDurationSeconds => Stages.Sum(s => s.DurationSeconds);

    /// <summary>
    /// Validates that the profile is valid
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               Stages.Count > 0 &&
               Stages.All(s => s.IsValid());
    }
}
