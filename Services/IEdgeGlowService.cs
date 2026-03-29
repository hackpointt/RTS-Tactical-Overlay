using System.Windows.Media;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service interface for managing edge glow breathing effect
/// </summary>
public interface IEdgeGlowService
{
    /// <summary>
    /// Starts the breathing glow with specified color and settings
    /// Called when execution starts or Stage changes
    /// </summary>
    void StartGlow(Color color, EdgeGlowSettings settings);

    /// <summary>
    /// Updates the glow color (instant, no animation restart)
    /// Called when switching to a new Stage
    /// </summary>
    void UpdateColor(Color color);

    /// <summary>
    /// Pauses the breathing animation at current brightness
    /// Called when execution is paused
    /// </summary>
    void PauseGlow();

    /// <summary>
    /// Resumes the breathing animation from current brightness
    /// Called when execution resumes
    /// </summary>
    void ResumeGlow();

    /// <summary>
    /// Stops the glow and fades out
    /// Called when execution stops or completes
    /// </summary>
    void StopGlow();

    /// <summary>
    /// Initializes the glow windows (called on startup)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up resources (called on exit)
    /// </summary>
    void Dispose();
}