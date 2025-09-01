using System;
using System.Windows.Data;

namespace LabelVal.Converters;

[ValueConversion(typeof(string), typeof(float))]
internal class FloatStringTrimConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        //int decimals = 3;
        //if (parameter != null)
        //    decimals = (int)parameter;

        float.TryParse(value.ToString(), out var floatValue) ? floatValue : value;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value;
}

