using OMRON.Reader.SDK.KCommands.Models;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V430.Views.TemplateSelectors;
public class KCommandTemplateSelector : DataTemplateSelector
{
    public required DataTemplate KCommandSelectTemplate { get; set; }
    public required DataTemplate KCommandRangeTemplate { get; set; }
    public required DataTemplate KCommandStringTemplate { get; set; }
    public required DataTemplate KCommandCharArrayTemplate { get; set; }
    public required DataTemplate KCommandCollectionTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is Field field)
        {
            return field.ArrayLength != null
                ? null
                : field.Editor switch
                {
                    "select" => KCommandSelectTemplate,
                    "range" => KCommandRangeTemplate,
                    "string" or "ipaddr" or "readonly" or "bit-field" => KCommandStringTemplate,
                    "chararray" or "hexarray" => KCommandCharArrayTemplate,
                    _ => base.SelectTemplate(item, container),
                };
        }
        return base.SelectTemplate(item, container);
    }
}
