using LabelVal.Results.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Helpers;

public class ImageResultDeviceEntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate V5Template { get; set; }
    public DataTemplate V275Template { get; set; }
    public DataTemplate L95Template { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) => item is ImageResultDeviceEntry_V5
            ? V5Template
            : item is ImageResultDeviceEntryV275
            ? V275Template
            : item is ImageResultDeviceEntry_L95 ? L95Template : base.SelectTemplate(item, container);
}