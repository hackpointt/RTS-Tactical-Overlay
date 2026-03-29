using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Type of macro action
/// </summary>
public enum MacroActionType
{
    KeyPress,   // Press and release
    KeyDown,    // Press only
    KeyUp,      // Release only
    Delay       // Wait
}

/// <summary>
/// JSON converter for nullable char
/// </summary>
public class NullableCharConverter : JsonConverter<char?>
{
    public override char? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return string.IsNullOrEmpty(str) ? null : str[0];
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, char? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString());
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Represents a single action within a macro
/// </summary>
public class MacroAction
{
    /// <summary>
    /// Type of action to perform
    /// </summary>
    public MacroActionType Type { get; set; } = MacroActionType.KeyPress;

    /// <summary>
    /// Key character (for simple keys like '1', 'a', etc.)
    /// </summary>
    [JsonConverter(typeof(NullableCharConverter))]
    public char? Key { get; set; }

    /// <summary>
    /// Virtual key code (for special keys like F1, Enter, etc.)
    /// </summary>
    public ushort? VirtualKeyCode { get; set; }

    /// <summary>
    /// Modifier key codes to hold during this action (Ctrl=17, Shift=16, Alt=18)
    /// </summary>
    public List<ushort>? Modifiers { get; set; }

    /// <summary>
    /// Delay in milliseconds (for Delay type, or delay after key action)
    /// </summary>
    public int DelayMs { get; set; } = 50;

    /// <summary>
    /// Validates that this action has required properties set
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Type switch
    {
        MacroActionType.Delay => DelayMs > 0,
        _ => Key.HasValue || VirtualKeyCode.HasValue
    };

    /// <summary>
    /// Gets a display string for this action
    /// </summary>
    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (Type == MacroActionType.Delay)
                return $"Delay {DelayMs}ms";

            var modText = "";
            if (Modifiers?.Count > 0)
            {
                var modNames = Modifiers.Select(GetModifierName).Where(n => n != null);
                modText = string.Join("+", modNames) + "+";
            }

            var keyText = Key?.ToString() ?? GetVirtualKeyName(VirtualKeyCode ?? 0);
            var typeText = Type switch
            {
                MacroActionType.KeyDown => " (Down)",
                MacroActionType.KeyUp => " (Up)",
                _ => ""
            };

            return $"{modText}{keyText}{typeText}";
        }
    }

    private static string? GetModifierName(ushort vk) => vk switch
    {
        0x10 => "Shift",
        0x11 => "Ctrl",
        0x12 => "Alt",
        _ => null
    };

    private static string GetVirtualKeyName(ushort vk) => vk switch
    {
        0x0D => "Enter",
        0x1B => "Esc",
        0x20 => "Space",
        0x09 => "Tab",
        >= 0x70 and <= 0x7B => $"F{vk - 0x70 + 1}",
        _ => $"VK_{vk:X2}"
    };
}
