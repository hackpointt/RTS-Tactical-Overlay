using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Native;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Records keyboard input for macro creation
/// </summary>
public class MacroRecorderService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Methods.LowLevelKeyboardProc? _hookCallback;
    private readonly List<RecordedKey> _recordedKeys = new();
    private readonly HashSet<int> _pressedModifiers = new();
    private Stopwatch? _stopwatch;
    private long _lastKeyTime;

    public bool IsRecording { get; private set; }
    public event EventHandler<RecordedKey>? KeyRecorded;
    public event EventHandler? RecordingStopped;

    public void StartRecording()
    {
        if (IsRecording) return;

        _recordedKeys.Clear();
        _pressedModifiers.Clear();
        _stopwatch = Stopwatch.StartNew();
        _lastKeyTime = 0;

        _hookCallback = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        if (curModule != null)
        {
            _hookId = Win32Methods.SetWindowsHookEx(
                Win32Constants.WH_KEYBOARD_LL,
                _hookCallback,
                Win32Methods.GetModuleHandle(curModule.ModuleName),
                0);
            IsRecording = _hookId != IntPtr.Zero;
        }
    }

    public List<MacroAction> StopRecording()
    {
        if (!IsRecording) return new List<MacroAction>();

        Win32Methods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        IsRecording = false;
        _stopwatch?.Stop();

        RecordingStopped?.Invoke(this, EventArgs.Empty);
        return ConvertToMacroActions();
    }

    public void CancelRecording()
    {
        if (!IsRecording) return;

        Win32Methods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        IsRecording = false;
        _stopwatch?.Stop();
        _recordedKeys.Clear();

        RecordingStopped?.Invoke(this, EventArgs.Empty);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsRecording)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool isKeyDown = wParam == (IntPtr)Win32Constants.WM_KEYDOWN ||
                            wParam == (IntPtr)Win32Constants.WM_SYSKEYDOWN;
            bool isKeyUp = wParam == (IntPtr)Win32Constants.WM_KEYUP ||
                          wParam == (IntPtr)Win32Constants.WM_SYSKEYUP;

            // Track modifier state
            if (IsModifierKey(vkCode))
            {
                int normalizedMod = NormalizeModifier(vkCode);
                if (isKeyDown) _pressedModifiers.Add(normalizedMod);
                else if (isKeyUp) _pressedModifiers.Remove(normalizedMod);
            }
            // Record non-modifier key presses
            else if (isKeyDown)
            {
                // Escape stops recording
                if (vkCode == 0x1B) // VK_ESCAPE
                {
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                        new Action(() => StopRecording()));
                    return Win32Methods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                RecordKey(vkCode);
            }
        }

        return Win32Methods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void RecordKey(int vkCode)
    {
        long currentTime = _stopwatch?.ElapsedMilliseconds ?? 0;
        int delay = (int)(currentTime - _lastKeyTime);
        _lastKeyTime = currentTime;

        var recorded = new RecordedKey
        {
            VirtualKeyCode = (ushort)vkCode,
            Modifiers = new List<ushort>(_pressedModifiers.Count),
            DelayMs = delay > 10 ? delay : 50
        };

        // Copy current modifiers
        foreach (var mod in _pressedModifiers)
            recorded.Modifiers.Add((ushort)mod);

        _recordedKeys.Add(recorded);
        KeyRecorded?.Invoke(this, recorded);
    }

    private List<MacroAction> ConvertToMacroActions()
    {
        var actions = new List<MacroAction>();
        bool isFirst = true;

        foreach (var key in _recordedKeys)
        {
            // Add delay between keys (skip first)
            if (!isFirst && key.DelayMs > 20)
            {
                actions.Add(new MacroAction
                {
                    Type = MacroActionType.Delay,
                    DelayMs = Math.Min(key.DelayMs, 2000)
                });
            }
            isFirst = false;

            // Add key press
            var action = new MacroAction
            {
                Type = MacroActionType.KeyPress,
                VirtualKeyCode = key.VirtualKeyCode,
                DelayMs = 50
            };

            // Convert VK to char if printable
            char c = VkToChar(key.VirtualKeyCode);
            if (c != '\0')
            {
                action.Key = c;
                action.VirtualKeyCode = null;
            }

            // Add modifiers
            if (key.Modifiers.Count > 0)
                action.Modifiers = new List<ushort>(key.Modifiers);

            actions.Add(action);
        }

        return actions;
    }

    private static bool IsModifierKey(int vkCode) =>
        vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12 || // Shift, Ctrl, Alt
        vkCode == 0xA0 || vkCode == 0xA1 || // LShift, RShift
        vkCode == 0xA2 || vkCode == 0xA3 || // LCtrl, RCtrl
        vkCode == 0xA4 || vkCode == 0xA5;   // LAlt, RAlt

    private static int NormalizeModifier(int vkCode) => vkCode switch
    {
        0xA0 or 0xA1 => 0x10, // LShift/RShift -> Shift
        0xA2 or 0xA3 => 0x11, // LCtrl/RCtrl -> Ctrl
        0xA4 or 0xA5 => 0x12, // LAlt/RAlt -> Alt
        _ => vkCode
    };

    private static char VkToChar(ushort vkCode)
    {
        // Numbers 0-9
        if (vkCode >= 0x30 && vkCode <= 0x39)
            return (char)vkCode;
        // Letters A-Z
        if (vkCode >= 0x41 && vkCode <= 0x5A)
            return char.ToLower((char)vkCode);
        // Numpad 0-9
        if (vkCode >= 0x60 && vkCode <= 0x69)
            return (char)('0' + (vkCode - 0x60));

        return '\0';
    }

    public void Dispose()
    {
        if (IsRecording)
            CancelRecording();
    }
}

/// <summary>
/// Represents a recorded key press
/// </summary>
public class RecordedKey
{
    public ushort VirtualKeyCode { get; set; }
    public List<ushort> Modifiers { get; set; } = new();
    public int DelayMs { get; set; }

    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            foreach (var mod in Modifiers)
            {
                parts.Add(mod switch
                {
                    0x10 or 0xA0 or 0xA1 => "Shift",
                    0x11 or 0xA2 or 0xA3 => "Ctrl",
                    0x12 or 0xA4 or 0xA5 => "Alt",
                    _ => ""
                });
            }

            char c = VkToChar(VirtualKeyCode);
            parts.Add(c != '\0' ? c.ToString().ToUpper() : $"VK_{VirtualKeyCode:X2}");

            return string.Join("+", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }

    private static char VkToChar(ushort vkCode)
    {
        if (vkCode >= 0x30 && vkCode <= 0x39) return (char)vkCode;
        if (vkCode >= 0x41 && vkCode <= 0x5A) return (char)vkCode;
        if (vkCode >= 0x60 && vkCode <= 0x69) return (char)('0' + (vkCode - 0x60));
        return '\0';
    }
}
