namespace RTS_Tactical_Overlay.Services;

public interface INativeInputSimulator
{
    void SendKeyPress(char key);
    void SendKeyPress(ushort virtualKeyCode);
    void SendKeyCombo(ushort[] modifiers, char key);
    void SendKeyDown(ushort virtualKeyCode);
    void SendKeyUp(ushort virtualKeyCode);
}
