using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class EnumValueToDescription_ShortenString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        try
        {
            var ret = value == null
                ? null
                : value is Enum
                    ? $"{(value.GetType()?.GetField(value.ToString() is string s ? s : string.Empty)?.GetCustomAttributes(typeof(System.ObsoleteAttribute), false).FirstOrDefault() is System.ObsoleteAttribute obsolete ? "*" : "")}{(value.GetType()?.GetField(value.ToString() is string s1 ? s1 : string.Empty)?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).FirstOrDefault() as System.ComponentModel.DescriptionAttribute)?.Description}"
                    : (object?)value;

            if (parameter is not int MaxLength)
                MaxLength = 13;

            if (ret is string s2)
            {
                var len = s2.Length;
                if (len <= MaxLength)
                {
                    return s2;
                }
                else
                    len = MaxLength;
                return s2[..len] + "...";
            }

            return value;

        }
        catch (Exception)
        {
            return value is Enum ? value.ToString() : value;
        }
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
