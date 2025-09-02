using System;
using System.Linq;
using System.Windows.Data;

namespace LabelVal.Converters
{
    [ValueConversion(typeof(System.Collections.IEnumerable), typeof(bool))]
    public class IsEnumerationNullOrEmpty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null || ((System.Collections.IEnumerable)value).Cast<object>().Count() == 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
