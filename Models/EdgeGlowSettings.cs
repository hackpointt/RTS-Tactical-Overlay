using System.Text.Json.Serialization;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Configuration for the edge glow breathing effect
/// </summary>
public class EdgeGlowSettings
{
    /// <summary>
    /// Breathing cycle duration in seconds (0.5 - 5.0)
    /// Lower = faster breathing, higher = slower
    /// </summary>
    public double BreathSpeedSeconds { get; set; } = 2.0;

    /// <summary>
    /// Maximum brightness/opacity (0.1 - 1.0)
    /// Controls how bright the glow can get at peak
    /// </summary>
    public double MaxIntensity { get; set; } = 0.6;

    /// <summary>
    /// Width of the glow band in pixels (10 - 100)
    /// Thicker = more visible, thinner = subtle
    /// </summary>
    public int GlowWidthPixels { get; set; } = 30;

    /// <summary>
    /// Whether the edge glow feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates default settings
    /// </summary>
    [JsonIgnore]
    public static EdgeGlowSettings Default => new EdgeGlowSettings();

    /// <summary>
    /// Clamps all values to valid ranges
    /// Called when loading from JSON to ensure safety
    /// </summary>
    public void ClampValues()
    {
        BreathSpeedSeconds = Math.Clamp(BreathSpeedSeconds, 0.5, 5.0);
        MaxIntensity = Math.Clamp(MaxIntensity, 0.1, 1.0);
        GlowWidthPixels = Math.Clamp(GlowWidthPixels, 10, 100);
    }
}