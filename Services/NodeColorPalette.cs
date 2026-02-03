using System.Windows.Media;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Provides a modern, vibrant color palette for timeline nodes
/// </summary>
public static class NodeColorPalette
{
    /// <summary>
    /// 20 carefully selected colors optimized for visibility and aesthetics
    /// Colors are vibrant, distinct, and work well on dark backgrounds
    /// </summary>
    private static readonly Color[] Colors = new[]
    {
        Color.FromRgb(0x00, 0xD9, 0xFF), // 1. Cyan
        Color.FromRgb(0xFF, 0x6B, 0x6B), // 2. Coral Red
        Color.FromRgb(0x4E, 0xFF, 0x4E), // 3. Lime Green
        Color.FromRgb(0xFF, 0xB3, 0x00), // 4. Amber
        Color.FromRgb(0xBB, 0x86, 0xFC), // 5. Purple
        Color.FromRgb(0xFF, 0x4D, 0xC4), // 6. Hot Pink
        Color.FromRgb(0x00, 0xFF, 0xC8), // 7. Turquoise
        Color.FromRgb(0xFF, 0xE5, 0x4D), // 8. Yellow
        Color.FromRgb(0x5C, 0xD1, 0xFF), // 9. Sky Blue
        Color.FromRgb(0xFF, 0x7F, 0x50), // 10. Orange
        Color.FromRgb(0x9D, 0xFF, 0x9D), // 11. Mint Green
        Color.FromRgb(0xFF, 0x99, 0xCC), // 12. Rose Pink
        Color.FromRgb(0x00, 0xE6, 0x76), // 13. Emerald
        Color.FromRgb(0xFF, 0xCC, 0x66), // 14. Gold
        Color.FromRgb(0xA7, 0xC7, 0xFF), // 15. Periwinkle
        Color.FromRgb(0xFF, 0x66, 0x99), // 16. Magenta
        Color.FromRgb(0x66, 0xFF, 0xCC), // 17. Aquamarine
        Color.FromRgb(0xFF, 0xAA, 0x4D), // 18. Peach
        Color.FromRgb(0xCC, 0xB3, 0xFF), // 19. Lavender
        Color.FromRgb(0xFF, 0xFF, 0x66)  // 20. Lemon
    };

    /// <summary>
    /// Gets a color for the specified node index (0-based)
    /// Colors cycle after 20 nodes
    /// </summary>
    public static Color GetColorForIndex(int index)
    {
        if (index < 0) return Colors[0];
        return Colors[index % Colors.Length];
    }

    /// <summary>
    /// Gets the total number of colors in the palette
    /// </summary>
    public static int ColorCount => Colors.Length;
}
