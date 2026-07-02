using System;
using System.Globalization;
using System.Windows.Data;

namespace HexWide
{
    public class WidthToFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double actualWidth)
                return 14.0;

            double baseSize = 14.0;
            if (parameter is string param && double.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                baseSize = parsed;

            // Scale relative to window width with reasonable min/max bounds.
            double scale = actualWidth / 920.0;
            double fontSize = Math.Round(baseSize * scale, 0);
            return Math.Min(Math.Max(fontSize, Math.Max(10.0, baseSize * 0.75)), Math.Max(baseSize, 42.0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
