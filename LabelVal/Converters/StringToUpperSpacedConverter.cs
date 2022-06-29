using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.Converters
{
    internal class StringToUpperSpacedConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return value;

            string tmp = Regex.Replace(
        Regex.Replace(
            value.ToString(),
            @"(\P{Ll})(\P{Ll}\p{Ll})",
            "$1 $2"
        ),
        @"(\p{Ll})(\P{Ll})",
        "$1 $2"
    );
            return $"{char.ToUpper(tmp[0])}{tmp.Substring(1)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

