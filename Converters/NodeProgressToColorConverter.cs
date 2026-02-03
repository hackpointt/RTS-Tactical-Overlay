using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RTS_Tactical_Overlay.Converters;

/// <summary>
/// Converts node progress (0.0-1.0) to fill/stroke color for gradual lighting effect
/// </summary>
public class NodeProgressToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double progress)
        {
            return new SolidColorBrush(Colors.Transparent);
        }

        // Clamp progress between 0 and 1
        progress = Math.Max(0.0, Math.Min(1.0, progress));

        if (progress < 0.01)
        {
            // Not started - very dim gray
            return new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));
        }
        else if (progress >= 1.0)
        {
            // Fully lit - bright cyan
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xCC, 0xFF));
        }
        else
        {
            // In progress - interpolate from dim to bright
            byte alpha = (byte)(0x30 + (0xCF * progress));
            return new SolidColorBrush(Color.FromArgb(alpha, 0x00, 0xCC, 0xFF));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
