using LabelVal.Sectors.Interfaces;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Wpf.lib.Extentions;

namespace LabelVal.ImageRolls.Converters;

public class GS1TableNamesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is GS1TableNames enumValue ? enumValue.GetDescription() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            foreach (GS1TableNames desc in Enum.GetValues(typeof(GS1TableNames)).Cast<GS1TableNames>().ToList())
            {
                if (desc.GetDescription() == strValue)
                    return desc;
            }
        }

        return value;
    }
}
