using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.Converters
{

    [ValueConversion(typeof(object), typeof(ContentControl))]
    public class ContentGeneratorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var control = new ContentControl { ContentTemplate = (DataTemplate)parameter };
            _ = control.SetBinding(ContentControl.ContentProperty, new Binding());
            return control;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
