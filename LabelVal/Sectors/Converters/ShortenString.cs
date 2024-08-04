using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class ShortenString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            int len = s.Length;
            if (len <= 12)
            {
                return s;
            }
            else
                len = 12;
            return s[..len] + "...";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
