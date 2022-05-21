using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace V275_Testing.Converters
{
    internal class StringToUpperSpacedConverter: IValueConverter
		{

			public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
			if (value == null || string.IsNullOrEmpty(value.ToString()))
				return value;
				string tmp = string.Concat(value.ToString().Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
				return $"{char.ToUpper(tmp[0])}{tmp.Substring(1)}";
			}

			public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				return value;
			}
		}
	}

