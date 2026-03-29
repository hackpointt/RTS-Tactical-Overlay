using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents a macro - a sequence of key actions
/// </summary>
public class Macro
{
    /// <summary>
    /// Optional name for this macro
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// List of actions to execute in sequence
    /// </summary>
    public List<MacroAction> Actions { get; set; } = new();

    /// <summary>
    /// Validates that this macro has valid actions
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Actions.Count > 0 && Actions.All(a => a.IsValid);

    /// <summary>
    /// Gets display text for this macro (first key or "M")
    /// </summary>
    public string GetDisplayText()
    {
        if (Actions.Count == 0)
            return "M";

        // Find first key action
        var firstKeyAction = Actions.FirstOrDefault(a =>
            a.Type != MacroActionType.Delay && (a.Key.HasValue || a.VirtualKeyCode.HasValue));

        if (firstKeyAction == null)
            return "M";

        if (firstKeyAction.Key.HasValue)
            return firstKeyAction.Key.Value.ToString();

        // For virtual keys, return abbreviated form
        return "M";
    }

    /// <summary>
    /// Gets a summary description of the macro
    /// </summary>
    [JsonIgnore]
    public string Summary
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;

            if (Actions.Count == 0)
                return "(empty)";

            if (Actions.Count == 1)
                return Actions[0].DisplayText;

            return $"{Actions[0].DisplayText} +{Actions.Count - 1}";
        }
    }
}
