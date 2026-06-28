using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TCC.UI.Converters;

public class ClassSkillSlotTransformConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var count = ToInt(values.Length > 0 ? values[0] : null);
        var index = ToInt(values.Length > 1 ? values[1] : null);
        var offset = GetOffset(parameter as string, count, index);
        return new TranslateTransform(offset.X, offset.Y);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static Point GetOffset(int count, int index)
    {
        return count switch
        {
            <= 1 => new Point(0, 90),
            2 when index == 1 => new Point(-45, 45),
            _ when index == 1 => new Point(-45, 45),
            _ when index >= 2 => new Point(0, 90),
            _ => new Point(45, 45)
        };
    }

    public static Point GetOffset(string? profile, int count, int index)
    {
        return string.Equals(profile, "Sorcerer", StringComparison.OrdinalIgnoreCase)
            ? GetSorcererOffset(count, index)
            : GetOffset(count, index);
    }

    private static Point GetSorcererOffset(int count, int index)
    {
        return count switch
        {
            <= 1 => new Point(0, 0),
            2 when index == 1 => new Point(42, 0),
            _ => new Point(-42, 0)
        };
    }

    private static int ToInt(object? value)
    {
        return value switch
        {
            int i => i,
            double d => (int)d,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
            _ => 0
        };
    }
}
