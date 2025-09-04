using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LabelVal.Sectors.Converters;

public class SectorReportToBrush : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return DependencyProperty.UnsetValue;

        var hasWarning = values[0] is bool bWarning && bWarning;
        var hasError = values[1] is bool bError && bError;

        if (hasError)
        {
            return Application.Current.TryFindResource("SectorError_Brush_Active") ?? Brushes.Transparent;
        }

        if (hasWarning)
        {
            return Application.Current.TryFindResource("SectorWarning_Brush_Active") ?? Brushes.Transparent;
        }

        return Application.Current.TryFindResource("MahApps.Brushes.Gray2") ?? Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}