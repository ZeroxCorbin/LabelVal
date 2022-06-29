using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.Converters
{
    internal class FloatStringTrimConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //int decimals = 3;
            //if (parameter != null)
            //    decimals = (int)parameter;

            if(float.TryParse(value.ToString(), out var floatValue))
            {
                return floatValue;
            }
            else
                return value;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

