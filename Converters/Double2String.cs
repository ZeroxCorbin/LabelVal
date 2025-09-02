using System;
using System.Windows.Data;

namespace LabelVal.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    public class Double2String : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is not double
                ? value
                : parameter is string format ? ((double)value).ToString(format) : (object)((double)value).ToString("F0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is not string str ? value : double.TryParse(str, out var result) ? result : value;
        }
    }
}
