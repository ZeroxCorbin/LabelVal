using System;
using System.Windows.Data;

namespace LabelVal.Converters;
internal class ObjectIsNull : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value == null;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value;
}
