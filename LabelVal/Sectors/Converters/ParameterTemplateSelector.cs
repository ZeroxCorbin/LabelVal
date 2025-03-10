using BarcodeVerification.lib.ISO;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Sectors.Converters;

public class ParameterTemplateSelector : DataTemplateSelector
{
    public DataTemplate ValueDoubleTemplate { get; set; }
    public DataTemplate ValueStringTemplate { get; set; }
    public DataTemplate PassFailTemplate { get; set; }
    public DataTemplate ValuePassFailTemplate { get; set; }
    public DataTemplate GradeTemplate { get; set; }
    public DataTemplate GradeValueTemplate { get; set; }
    public DataTemplate Missing { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is ValueDouble)
            return ValueDoubleTemplate;
        else if (item is ValueString)
            return ValueStringTemplate;
        else if (item is PassFail)
            return PassFailTemplate;
        else if (item is ValuePassFail)
            return ValuePassFailTemplate;
        else if (item is Grade)
            return GradeTemplate;
        else if (item is GradeValue)
            return GradeValueTemplate;
        else if (item is Missing)
            return Missing;

        return base.SelectTemplate(item, container);
    }
}
