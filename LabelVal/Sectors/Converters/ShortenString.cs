using System.Globalization;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class ShortenString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not int MaxLength)
            MaxLength = 13;

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
