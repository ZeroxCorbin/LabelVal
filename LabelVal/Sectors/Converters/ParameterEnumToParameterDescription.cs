using BarcodeVerification.lib.ISO;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class ParameterEnumToParameterDescription : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is AvailableParameters parameterEnum)
        {
            return parameterEnum.GetParameterDescription();
        }
        return string.Empty;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
