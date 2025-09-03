using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.Converters
{
    class MACAddressSplitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Split the MAC address into 2 parts. First and last three octets.
            //If the parameter is set to "First" return the first three octets with the ... folowing the octetcs seperated by a colon.
            //If the parameter is set to "Last" return the last three octets with the ... preceeding the octets seperated by a colon.
            if (value == null || !(value is string macAddress) || macAddress.Length != 17)
                return null;

            var split = macAddress.Split(':');
            if (split.Length != 6)
                return null;

            if (parameter == null || !(parameter is string param))
                return null;

            if (param == "First")
                return $"{split[0]}:{split[1]}:{split[2]}..";
            else if (param == "Last")
                return $"..{split[3]}:{split[4]}:{split[5]}";
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
