using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToPassFailString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is bool ? (bool)value ? "Pass" : "Fail" : (object)"Fail";
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is string ? (string)value == "Pass" : (object)false;
}
