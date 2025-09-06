using BarcodeVerification.lib.ISO.ParameterTypes;
using LabelVal.Sectors.Classes;
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
    public DataTemplate OverallGradeTemplate { get; set; }
    public DataTemplate Missing { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if(item is SectorElement element)
        {
            if (element.DataType == typeof(ValueDouble))
                return ValueDoubleTemplate;
            else if (element.DataType == typeof(ValueString))
                return ValueStringTemplate;
            else if (element.DataType == typeof(PassFail))
                return PassFailTemplate;
            else if (element.DataType == typeof(ValuePassFail))
                return ValuePassFailTemplate;
            else if (element.DataType == typeof(Grade))
                return GradeTemplate;
            else if (element.DataType == typeof(GradeValue))
                return GradeValueTemplate;
            else if (element.DataType == typeof(OverallGrade))
                return OverallGradeTemplate;
            else if (element.DataType == typeof(Missing))
                return Missing;

        }
        else
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
            else if (item is OverallGrade)
                return OverallGradeTemplate;
            else if (item is Missing)
                return Missing;
        }
        return base.SelectTemplate(item, container);
    }
}
