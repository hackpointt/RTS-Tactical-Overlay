using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RTS_Tactical_Overlay.Converters;

/// <summary>
/// Converts IncomingLineProgress and NodeColor to a gradient brush for the connecting line
/// - Progress 0: Dim white line
/// - Progress 0-1: Gradient from node color to dim white
/// - Progress 1: Full node color line with glow
/// </summary>
public class LineColorProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is not double progress || values[1] is not Color nodeColor)
        {
            return new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 0)
        };

        if (progress < 0.01)
        {
            // Dim white line for pending connections
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 1.0));
        }
        else if (progress >= 1.0)
        {
            // Fully lit with node color
            brush.GradientStops.Add(new GradientStop(nodeColor, 0.0));
            brush.GradientStops.Add(new GradientStop(nodeColor, 1.0));
        }
        else
        {
            // Animated gradient: node color from start to progress point, then dim white
            brush.GradientStops.Add(new GradientStop(nodeColor, 0.0));
            brush.GradientStops.Add(new GradientStop(nodeColor, progress));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), progress));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 1.0));
        }

        return brush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
