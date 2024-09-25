using System;
using System.Windows.Data;

namespace LabelVal.Converters;

[ValueConversion(typeof(object), typeof(bool))]
internal class ObjectIsNotNull : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value != null;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value;
}

