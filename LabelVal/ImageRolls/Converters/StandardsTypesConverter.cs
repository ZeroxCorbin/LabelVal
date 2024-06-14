using LabelVal.Extensions;
using LabelVal.Sectors.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.ImageRolls.Converters
{
    public class StandardsTypesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StandardsTypes enumValue)
                return enumValue.GetDescription();

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                foreach (var desc in Enum.GetValues(typeof(StandardsTypes)).Cast<StandardsTypes>().ToList())
                {
                    if (desc.GetDescription() == strValue)
                        return desc;
                }
            }

            return StandardsTypes.None;
        }
    }
}
