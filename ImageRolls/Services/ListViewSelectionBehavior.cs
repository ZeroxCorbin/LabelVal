using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.ImageRolls.Services
{
    public static class ListViewSelectionBehavior
    {
        public static readonly DependencyProperty SelectionServiceProperty =
            DependencyProperty.RegisterAttached(
                "SelectionService",
                typeof(SelectionService),
                typeof(ListViewSelectionBehavior),
                new PropertyMetadata(null, OnSelectionServiceChanged));

        public static void SetSelectionService(DependencyObject element, SelectionService value)
        {
            element.SetValue(SelectionServiceProperty, value);
        }

        public static SelectionService GetSelectionService(DependencyObject element)
        {
            return (SelectionService)element.GetValue(SelectionServiceProperty);
        }

        private static void OnSelectionServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                if (e.OldValue is SelectionService oldService)
                {
                    // Unregister from old service if necessary
                }

                if (e.NewValue is SelectionService newService)
                {
                    newService.RegisterListView(listView);
                    listView.SelectionChanged += (sender, args) => newService.NotifySelectionChanged(listView);
                }
            }
        }
    }
}
