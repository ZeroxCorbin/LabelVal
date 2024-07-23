using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.Converters;
class IPAddressSplitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //Split the IP Address address into 2 parts. First and last two octets.
        //If the parameter is set to "First" return the first three octets with the ... folowing the octetcs seperated by a period.
        //If the parameter is set to "Last" return the last three octets with the ... preceeding the octets seperated by a period.
        if (value == null || !(value is string macAddress))
            return null;

        string[] split = macAddress.Split('.');
        if (split.Length != 4)
            return null;

        if (parameter == null || !(parameter is string param))
            return null;

        if (param == "First")
            return $"{split[0]}.{split[1]}.";
        else if (param == "Last")
            return $".{split[2]}.{split[3]}";
        else
            return null;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
