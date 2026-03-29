using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.ViewModels;

/// <summary>
/// ViewModel for editing a macro action
/// </summary>
public partial class MacroActionViewModel : ObservableObject
{
    [ObservableProperty]
    private MacroActionType _type = MacroActionType.KeyPress;

    [ObservableProperty]
    private string _keyText = "";

    [ObservableProperty]
    private int _delayMs = 50;

    [ObservableProperty]
    private bool _useCtrl;

    [ObservableProperty]
    private bool _useShift;

    [ObservableProperty]
    private bool _useAlt;

    [ObservableProperty]
    private bool _isEditing;

    public string DisplayText
    {
        get
        {
            var text = ToMacroAction().DisplayText;
            return IsEditing ? $"[Editing] {text}" : text;
        }
    }

    public MacroAction ToMacroAction()
    {
        var action = new MacroAction
        {
            Type = Type,
            DelayMs = DelayMs
        };

        if (Type != MacroActionType.Delay && !string.IsNullOrEmpty(KeyText))
        {
            if (KeyText.Length == 1)
                action.Key = KeyText[0];
            else
                action.VirtualKeyCode = ParseVirtualKey(KeyText);
        }

        if (UseCtrl || UseShift || UseAlt)
        {
            action.Modifiers = new();
            if (UseCtrl) action.Modifiers.Add(0x11);
            if (UseShift) action.Modifiers.Add(0x10);
            if (UseAlt) action.Modifiers.Add(0x12);
        }

        return action;
    }

    public static MacroActionViewModel FromMacroAction(MacroAction action)
    {
        var vm = new MacroActionViewModel
        {
            Type = action.Type,
            DelayMs = action.DelayMs
        };

        if (action.Key.HasValue)
            vm.KeyText = action.Key.Value.ToString();
        else if (action.VirtualKeyCode.HasValue)
            vm.KeyText = GetVirtualKeyText(action.VirtualKeyCode.Value);

        if (action.Modifiers != null)
        {
            vm.UseCtrl = action.Modifiers.Contains((ushort)0x11);
            vm.UseShift = action.Modifiers.Contains((ushort)0x10);
            vm.UseAlt = action.Modifiers.Contains((ushort)0x12);
        }

        return vm;
    }

    private static ushort ParseVirtualKey(string text)
    {
        return text.ToUpper() switch
        {
            "ENTER" => 0x0D,
            "ESC" => 0x1B,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            _ => 0
        };
    }

    private static string GetVirtualKeyText(ushort vk)
    {
        return vk switch
        {
            0x0D => "Enter",
            0x1B => "Esc",
            0x20 => "Space",
            0x09 => "Tab",
            >= 0x70 and <= 0x7B => $"F{vk - 0x70 + 1}",
            _ => $"VK_{vk:X2}"
        };
    }
}
