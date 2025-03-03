using LabelVal.Sectors.Classes;
using System.Globalization;
using System.Windows.Data;
using Wpf.lib.Extentions;

namespace LabelVal.ImageRolls.Converters;

public class StandardsTypesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is StandardsTypes enumValue ? enumValue.GetDescription() : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            foreach (StandardsTypes desc in Enum.GetValues(typeof(StandardsTypes)).Cast<StandardsTypes>().ToList())
            {
                if (desc.GetDescription() == strValue)
                    return desc;
            }
        }

        return StandardsTypes.None;
    }
}
