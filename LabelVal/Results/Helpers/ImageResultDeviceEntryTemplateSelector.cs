using LabelVal.Results.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Helpers;

public class ResultsDeviceEntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate V5Template { get; set; }
    public DataTemplate V275Template { get; set; }
    public DataTemplate L95Template { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) => item is ResultsDeviceEntry_V5
            ? V5Template
            : item is ResultsDeviceEntryV275
            ? V275Template
            : item is ResultsDeviceEntry_L95 ? L95Template : base.SelectTemplate(item, container);
}