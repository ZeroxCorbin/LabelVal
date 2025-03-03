using BarcodeVerification.lib.GS1;
using System.Globalization;
using System.Windows.Data;
using Wpf.lib.Extentions;

namespace LabelVal.ImageRolls.Converters;

public class AvailableTablesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is AvailableTables enumValue ? enumValue.GetDescription() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            foreach (AvailableTables desc in Enum.GetValues(typeof(AvailableTables)).Cast<AvailableTables>().ToList())
            {
                if (desc.GetDescription() == strValue)
                    return desc;
            }
        }

        return value;
    }
}
