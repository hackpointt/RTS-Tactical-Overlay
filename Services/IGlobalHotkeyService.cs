using System;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service for managing global keyboard hooks
/// </summary>
public interface IGlobalHotkeyService
{
    /// <summary>
    /// Starts listening for global hotkeys
    /// </summary>
    void Start();

    /// <summary>
    /// Stops listening for global hotkeys
    /// </summary>
    void Stop();

    /// <summary>
    /// Event raised when a registered hotkey is pressed
    /// </summary>
    event EventHandler<string>? HotkeyPressed;

    /// <summary>
    /// Whether the service is currently running
    /// </summary>
    bool IsRunning { get; }
}
