using OMRON.Reader.SDK.KCommands.Models;
using System.Globalization;
using System.Windows.Data;

namespace LabelVal.V430.Views.TemplateSelectors;

public class KCommandCmd2IndexedCmds : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is OMRON.Reader.SDK.Helpers.ObservableDictionary<string, System.Collections.Generic.List<System.Collections.Generic.List<OMRON.Reader.SDK.KCommands.Models.Field>>> dict)
            if (values[1] is string s)
                if (dict.TryGetValue(s, out List<List<Field>> list))
                    return list;

        return values[0];
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
