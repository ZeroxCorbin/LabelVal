using BarcodeVerification.lib.Common;
using System.Globalization;
using System.Windows.Data;
using Wpf.lib.Extentions;

namespace LabelVal.ImageRolls.Converters;

public class AvailableStandardsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is AvailableStandards enumValue ? enumValue.GetDescription() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            foreach (AvailableStandards desc in Enum.GetValues(typeof(AvailableStandards)).Cast<AvailableStandards>().ToList())
            {
                if (desc.GetDescription() == strValue)
                    return desc;
            }
        }

        return null;
    }
}
