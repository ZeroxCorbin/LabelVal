using GS1.Encoders;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LabelVal.V275.Converters;

internal class V275_GS1FormattedOutput : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        try
        {
            var regex = new Regex(Regex.Escape("\u001d"));
            string scandata = (string)value;
            scandata = regex.Replace(scandata, "^");
            App.GS1Encoder.DataStr = "^" + scandata;
        }
        catch (Exception E)
        {
            if (!(E is GS1EncoderParameterException) && !(E is GS1EncoderScanDataException))
                throw;

            string markup = App.GS1Encoder.ErrMarkup;
            if (!markup.Equals(""))
            {
                var regex = new Regex(Regex.Escape("|"));
                markup = regex.Replace(markup, "⧚", 1);
                markup = regex.Replace(markup, "⧛", 1);
                return "AI content validation failed: " + markup;
            }

            return E.Message;
        }

        var sb = new System.Text.StringBuilder();
        foreach (string s in App.GS1Encoder.HRI)
        {
            sb.Append(s);
            sb.Append("\n");
        }

        return sb.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
