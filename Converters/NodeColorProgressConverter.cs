using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RTS_Tactical_Overlay.Converters;

/// <summary>
/// Converts NodeProgress and NodeColor to a color with appropriate brightness
/// - Progress 0: Dim version of the color (20% opacity)
/// - Progress 0-1: Gradually brighten to full color
/// - Progress 1: Full bright color
/// </summary>
public class NodeColorProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is not double progress || values[1] is not Color nodeColor)
        {
            return new SolidColorBrush(Colors.Gray);
        }

        if (progress < 0.01)
        {
            // Dim version: 20% opacity of the node color
            var dimColor = Color.FromArgb(0x33, nodeColor.R, nodeColor.G, nodeColor.B);
            return new SolidColorBrush(dimColor);
        }
        else if (progress >= 1.0)
        {
            // Full bright color
            return new SolidColorBrush(nodeColor);
        }
        else
        {
            // Gradual brightening: interpolate opacity from 20% to 100%
            byte alpha = (byte)(0x33 + (0xCC * progress));
            var interpolatedColor = Color.FromArgb(alpha, nodeColor.R, nodeColor.G, nodeColor.B);
            return new SolidColorBrush(interpolatedColor);
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
