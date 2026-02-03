using System.Collections.Generic;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Container for all hotkey configurations
/// </summary>
public class HotkeySettings
{
    public List<HotkeyConfig> Hotkeys { get; set; } = new();
}
