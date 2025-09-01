using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LabelVal.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    internal class StringCamelUnspaced2StringPascalSpaced : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is null or not string)
                return value;

            var tmp = Regex.Replace(Regex.Replace(value.ToString(), @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");

            if (string.IsNullOrEmpty(tmp))
                return value;

            return $"{char.ToUpper(tmp[0])}{tmp.Substring(1)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

