using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using LabelVal.Results.ViewModels;

namespace LabelVal.Results.Helpers
{
    public class ImageResultDeviceEntryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate V5Template { get; set; }
        public DataTemplate V275Template { get; set; }
        public DataTemplate L95Template { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ImageResultDeviceEntry_V5)
                return V5Template;
            if (item is ImageResultDeviceEntry_V275)
                return V275Template;
            if (item is ImageResultDeviceEntry_L95)
                return L95Template;

            return base.SelectTemplate(item, container);
        }
    }
}