using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using System.Windows.Data;

namespace LabelVal.Sectors.Converters;

public class ParameterEnumToParameterDescription : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Parameters parameterEnum)
        {
            return parameterEnum.GetDescription();
        }
        return string.Empty;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
