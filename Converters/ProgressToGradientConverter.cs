using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RTS_Tactical_Overlay.Converters;

/// <summary>
/// Converts progress value (0.0-1.0) to a gradient brush for animated line effect
/// </summary>
public class ProgressToGradientConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double progress)
        {
            return new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)); // Dim default
        }

        // Clamp progress between 0 and 1
        progress = Math.Max(0.0, Math.Min(1.0, progress));

        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 0)
        };

        if (progress <= 0.0)
        {
            // Not started - dim line
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 1.0));
        }
        else if (progress >= 1.0)
        {
            // Completed - fully bright cyan
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0xD9, 0xFF), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0xD9, 0xFF), 1.0));
        }
        else
        {
            // In progress - gradient from bright to dim
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0xD9, 0xFF), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0xD9, 0xFF), progress));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), progress));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 1.0));
        }

        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
