using System;
using System.Runtime.InteropServices;
using System.Threading;
using RTS_Tactical_Overlay.Native;

namespace RTS_Tactical_Overlay.Services;

public class NativeInputSimulator : INativeInputSimulator
{
    public void SendKeyPress(char key)
    {
        // Convert char to virtual key code
        ushort vk = (ushort)char.ToUpper(key);
        SendKeyPress(vk);
    }

    public void SendKeyPress(ushort virtualKeyCode)
    {
        SendKeyDown(virtualKeyCode);
        Thread.Sleep(10); // Small delay between down and up
        SendKeyUp(virtualKeyCode);
    }

    public void SendKeyCombo(ushort[] modifiers, char key)
    {
        // Press all modifiers
        foreach (var modifier in modifiers)
        {
            SendKeyDown(modifier);
            Thread.Sleep(5);
        }

        // Press the main key
        SendKeyPress(key);

        // Release all modifiers in reverse order
        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            SendKeyUp(modifiers[i]);
            Thread.Sleep(5);
        }
    }

    public void SendKeyDown(ushort virtualKeyCode)
    {
        var input = CreateKeyboardInput(virtualKeyCode, false);
        SendInputInternal(input);
    }

    public void SendKeyUp(ushort virtualKeyCode)
    {
        var input = CreateKeyboardInput(virtualKeyCode, true);
        SendInputInternal(input);
    }

    private INPUT CreateKeyboardInput(ushort virtualKeyCode, bool isKeyUp)
    {
        var scanCode = (ushort)Win32Methods.MapVirtualKey(virtualKeyCode, Win32Constants.MAPVK_VK_TO_VSC);
        return new INPUT
        {
            Type = Win32Constants.INPUT_KEYBOARD,
            Data = new InputUnion
            {
                Keyboard = new KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    wScan = scanCode,
                    dwFlags = isKeyUp ? Win32Constants.KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private void SendInputInternal(INPUT input)
    {
        var inputs = new[] { input };
        var result = Win32Methods.SendInput(1, inputs, Marshal.SizeOf<INPUT>());

        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput failed with error code: {error}");
        }
    }
}
