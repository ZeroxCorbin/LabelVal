using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.ImageRolls.Views
{
    /// <summary>
    /// Chooses a non-selectable style for grouping levels (CollectionViewGroup)
    /// and a normal selectable style for leaf ImageRoll items.
    /// </summary>
    public class ImageRollsTreeViewItemStyleSelector : StyleSelector
    {
        public Style? GroupStyle { get; set; }
        public Style? ItemStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is CollectionViewGroup)
            {
                return GroupStyle ?? ItemStyle ?? base.SelectStyle(item, container);
            }

            // Leaf (ImageRoll) or anything else
            return ItemStyle ?? base.SelectStyle(item, container);
        }
    }
}