using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelVal.Converters;

[ValueConversion(typeof(bool), typeof(bool))]
public class BoolInvert : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool v ? !v : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool v ? !v : value;
}