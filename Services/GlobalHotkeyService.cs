using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Native;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Implements global keyboard hook for hotkey detection
/// </summary>
public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Methods.LowLevelKeyboardProc? _hookCallback;
    private Dictionary<int, HotkeyConfig> _hotkeyMap = new();
    private HashSet<int> _pressedKeys = new();

    public bool IsRunning { get; private set; }
    public event EventHandler<string>? HotkeyPressed;

    public GlobalHotkeyService()
    {
        LoadHotkeyConfiguration();
    }

    private void LoadHotkeyConfiguration()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotkeys.json");
            Console.WriteLine($"[LoadHotkeyConfig] Looking for config at: {configPath}");
            Console.WriteLine($"[LoadHotkeyConfig] File exists: {File.Exists(configPath)}");

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                Console.WriteLine($"[LoadHotkeyConfig] JSON content length: {json.Length}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var settings = JsonSerializer.Deserialize<HotkeySettings>(json, options);

                Console.WriteLine($"[LoadHotkeyConfig] Settings loaded: {settings != null}");
                if (settings != null)
                {
                    Console.WriteLine($"[LoadHotkeyConfig] Hotkeys count: {settings.Hotkeys.Count}");
                    foreach (var hotkey in settings.Hotkeys)
                    {
                        _hotkeyMap[hotkey.VirtualKeyCode] = hotkey;
                        Console.WriteLine($"[LoadHotkeyConfig] Registered: vkCode={hotkey.VirtualKeyCode}, action={hotkey.Action}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading hotkey config: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public void Start()
    {
        if (IsRunning) return;

        Console.WriteLine("[GlobalHotkeyService] Starting keyboard hook...");
        _hookCallback = HookCallback;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule != null)
        {
            _hookId = Win32Methods.SetWindowsHookEx(
                Win32Constants.WH_KEYBOARD_LL,
                _hookCallback,
                Win32Methods.GetModuleHandle(curModule.ModuleName),
                0);
            IsRunning = true;
            Console.WriteLine($"[GlobalHotkeyService] Hook installed: {_hookId != IntPtr.Zero}");
            Console.WriteLine($"[GlobalHotkeyService] Registered hotkeys: {_hotkeyMap.Count}");
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;

        Win32Methods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        IsRunning = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Console.WriteLine($"[Hook] Key event: vkCode={vkCode}, wParam={wParam}");

            if (wParam == (IntPtr)Win32Constants.WM_KEYDOWN || wParam == (IntPtr)Win32Constants.WM_SYSKEYDOWN)
            {
                _pressedKeys.Add(vkCode);
                CheckHotkey(vkCode);
            }
            else if (wParam == (IntPtr)Win32Constants.WM_KEYUP || wParam == (IntPtr)Win32Constants.WM_SYSKEYUP)
            {
                _pressedKeys.Remove(vkCode);
            }
        }

        return Win32Methods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void CheckHotkey(int vkCode)
    {
        if (_hotkeyMap.TryGetValue(vkCode, out var hotkey))
        {
            Console.WriteLine($"[CheckHotkey] Found hotkey for vkCode={vkCode}, action={hotkey.Action}");
            bool modifiersMatch = CheckModifiers(hotkey.Modifiers);
            Console.WriteLine($"[CheckHotkey] Modifiers match: {modifiersMatch}");
            if (modifiersMatch)
            {
                Console.WriteLine($"[CheckHotkey] Invoking action: {hotkey.Action}");
                HotkeyPressed?.Invoke(this, hotkey.Action);
            }
        }
    }

    private bool CheckModifiers(List<string> requiredModifiers)
    {
        if (requiredModifiers.Count == 0) return true;

        foreach (var modifier in requiredModifiers)
        {
            int vk = modifier.ToUpper() switch
            {
                "CTRL" => Win32Constants.VK_CONTROL,
                "ALT" => Win32Constants.VK_MENU,
                "SHIFT" => Win32Constants.VK_SHIFT,
                _ => 0
            };
            if (vk != 0 && !_pressedKeys.Contains(vk)) return false;
        }
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}
