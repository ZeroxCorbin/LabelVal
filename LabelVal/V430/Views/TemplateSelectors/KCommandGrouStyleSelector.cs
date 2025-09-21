using OMRON.Reader.SDK.KCommands.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace LabelVal.V430.Views.TemplateSelectors;

public class KCommandGrouStyleSelector : StyleSelector
{
    public override Style SelectStyle(object item, DependencyObject container)
    {
        if (item is CollectionViewGroup cvg)
        {
            if (cvg.Name.ToString().StartsWith("K"))
            {
                if (cvg.Items.Count > 0)
                    if (cvg.Items[0] is Field kCommand)
                        if (kCommand.ArrayLength != null)
                            return CollectionGroupStyle;

                return CardGroupStyle;
            }
        }
        return DefaultGroupStyle;
    }
    public required Style DefaultGroupStyle { get; set; }
    public required Style CardGroupStyle { get; set; }
    public required Style CollectionGroupStyle { get; set; }
}
