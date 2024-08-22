using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class ShortenString : IValueConverter
{
    private const int MaxLength = 13;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            int len = s.Length;
            if (len <= MaxLength)
            {
                return s;
            }
            else
                len = MaxLength;
            return s[..len] + "...";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
