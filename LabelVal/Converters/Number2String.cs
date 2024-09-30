using LabelVal.Extensions;
using System;
using System.Windows.Data;

namespace LabelVal.Converters
{
    internal class Number2String : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? "" : value.IsNumber() ? value.ToDouble().ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

