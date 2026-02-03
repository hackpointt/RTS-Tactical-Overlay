using System.Collections.Generic;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents a hotkey configuration
/// </summary>
public class HotkeyConfig
{
    /// <summary>
    /// Virtual key code (e.g., 0x74 for F5)
    /// </summary>
    public int VirtualKeyCode { get; set; }

    /// <summary>
    /// Modifier keys (Ctrl, Alt, Shift)
    /// </summary>
    public List<string> Modifiers { get; set; } = new();

    /// <summary>
    /// Action to perform when hotkey is pressed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
