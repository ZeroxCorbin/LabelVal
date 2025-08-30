using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace LabelVal.V275.Converters;

internal class V275_GS1FormattedOutput : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 &&
            values[0] != DependencyProperty.UnsetValue && values[1] != DependencyProperty.UnsetValue &&
            values[0] is string val && values[1] is Symbologies symbology)
        {
            try
            {
                if (val.StartsWith("^h", StringComparison.InvariantCultureIgnoreCase))
                    val = val.Replace("^", "");

                if(val.StartsWith("#"))
                    val = val.Replace("#", "^");

                if(!val.StartsWith("^"))
                    val = "^01" + val;

                App.GS1Encoder.Sym = symbology switch
                {
                    Symbologies.DataBarOmni => GS1Encoder.Symbology.DataBarOmni,
                    Symbologies.DataBarTruncated => GS1Encoder.Symbology.DataBarTruncated,
                    Symbologies.DataBarStacked => GS1Encoder.Symbology.DataBarStacked,
                    Symbologies.DataBarStackedOmni => GS1Encoder.Symbology.DataBarStackedOmni,
                    Symbologies.DataBarLimited => GS1Encoder.Symbology.DataBarLimited,
                    Symbologies.DataBarExpanded => GS1Encoder.Symbology.DataBarExpanded,
                    Symbologies.DataBarExpandedStacked => GS1Encoder.Symbology.DataBarExpanded,
                    Symbologies.UPCA => GS1Encoder.Symbology.UPCA,
                    Symbologies.UPCE => GS1Encoder.Symbology.UPCE,
                    Symbologies.EAN13 => GS1Encoder.Symbology.EAN13,
                    Symbologies.EAN8 => GS1Encoder.Symbology.EAN8,
                    Symbologies.Code128 => GS1Encoder.Symbology.GS1_128_CCA,
                    Symbologies.CC_A => GS1Encoder.Symbology.GS1_128_CCA,
                    Symbologies.CC_B => GS1Encoder.Symbology.GS1_128_CCA,
                    Symbologies.CC_C => GS1Encoder.Symbology.GS1_128_CCC,
                    Symbologies.QRCode => GS1Encoder.Symbology.QR,
                    Symbologies.DataMatrix => GS1Encoder.Symbology.DM,
                    _ => GS1Encoder.Symbology.NONE,
                };
                var rs = App.GS1Encoder.DataStr = val;
            }
            catch (Exception E)
            {
                if (E is not GS1EncoderParameterException and not GS1EncoderScanDataException)
                    throw;

                string markup = App.GS1Encoder.ErrMarkup;
                if (!markup.Equals(""))
                {
                    Regex regex = new(Regex.Escape("|"));
                    markup = regex.Replace(markup, "⧚", 1);
                    markup = regex.Replace(markup, "⧛", 1);
                    return "AI content validation failed:\n" + markup;
                }

                return E.Message;
            }

            System.Text.StringBuilder sb = new();
            int i = 0;
            foreach (string s in App.GS1Encoder.HRI)
            {
                if (i++ != 0)
                    _ = sb.Append("\n");

                _ = sb.Append(s);
            }

            return sb.ToString();
        }
        return "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
}