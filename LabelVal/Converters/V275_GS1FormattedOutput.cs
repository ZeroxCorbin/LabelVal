using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.V275.Converters
{
    internal class V275_GS1FormattedOutput : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = (string)value;

            var spl = val.Split('(');
            int i = 1;
            if (spl.Length != 1)
            {
                val = "\r\n";
                foreach (var s in spl)
                    if (!string.IsNullOrEmpty(s))
                    {
                        val += "(" + s;
                        if (i++ != spl.Count())
                            val += "\r\n";
                    }
                    else
                        i++;
                        
            }
 
            return val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
