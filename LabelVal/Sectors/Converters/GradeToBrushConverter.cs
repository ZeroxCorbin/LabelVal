using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LabelVal.Sectors.Converters;

public class GradeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string grade)
            return Brushes.Transparent;

        var keySuffix = parameter as string == "Active" ? "_Brush_Active" : "_Brush";

        return grade switch
        {
            "A" => Application.Current.TryFindResource($"ISO_GradeA{keySuffix}") ?? Brushes.Black,
            "B" => Application.Current.TryFindResource($"ISO_GradeB{keySuffix}") ?? Brushes.Black,
            "C" => Application.Current.TryFindResource($"ISO_GradeC{keySuffix}") ?? Brushes.Black,
            "D" => Application.Current.TryFindResource($"ISO_GradeD{keySuffix}") ?? Brushes.Black,
            "F" => Application.Current.TryFindResource($"ISO_GradeF{keySuffix}") ?? Brushes.Black,
            _ => Brushes.Transparent,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}