using LabelVal.Extensions;
using LabelVal.Sectors.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LabelVal.ImageRolls.Converters
{
    public class GS1TableNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GS1TableNames enumValue)
                return enumValue.GetDescription();
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                foreach(var desc in Enum.GetValues(typeof(GS1TableNames)).Cast<GS1TableNames>().ToList())
                {
                    if (desc.GetDescription() == strValue)
                        return desc;
                }
            }

            return value;
        }
    }
}
