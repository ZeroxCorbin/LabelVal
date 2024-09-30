using LabelVal.Extensions;
using System;
using System.Numerics;
using System.Windows.Data;

namespace LabelVal.Converters;

[ValueConversion(typeof(INumber<>), typeof(bool))]
internal class NumberZeroOrNull : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value == null ? true : value.IsNumber() ? value.ToDouble() == 0 : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
