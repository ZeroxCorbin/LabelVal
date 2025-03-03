using LabelVal.Sectors.Classes;
using System.Globalization;
using System.Windows.Data;
using Wpf.lib.Extentions;

namespace LabelVal.ImageRolls.Converters;

public class Gs1TableNamesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Gs1TableNames enumValue ? enumValue.GetDescription() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            foreach (Gs1TableNames desc in Enum.GetValues(typeof(Gs1TableNames)).Cast<Gs1TableNames>().ToList())
            {
                if (desc.GetDescription() == strValue)
                    return desc;
            }
        }

        return value;
    }
}
