using BarcodeVerification.lib.Common;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class ParameterSuffixToUnitsMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return (values[0] is not string s) || (values[1] is not AvailableUnits units)
            ? values[0]
            : s.Equals("<units>") ? units.GetDescription() : s;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
