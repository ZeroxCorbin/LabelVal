using System.Globalization;
using System.Windows.Data;

namespace LabelVal.Converters;
internal class IPAddressSplitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //Split the IP Address address into 2 parts. First and last two octets.
        //If the parameter is set to "First" return the first three octets with the ... folowing the octetcs seperated by a period.
        //If the parameter is set to "Last" return the last three octets with the ... preceeding the octets seperated by a period.
        if (value == null || value is not string macAddress)
            return null;

        var split = macAddress.Split('.');
        return split.Length != 4
            ? null
            : parameter == null || parameter is not string param
            ? null
            : param == "First" ? $"{split[0]}.{split[1]}." : param == "Last" ? $".{split[2]}.{split[3]}" : (object)null;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
