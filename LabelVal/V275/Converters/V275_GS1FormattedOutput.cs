using BarcodeVerification.lib.GS1.Encoders;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LabelVal.V275.Converters;

internal class V275_GS1FormattedOutput : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string val)
        {
            try
            {
                if (val.StartsWith("^h", StringComparison.InvariantCultureIgnoreCase))
                    val = val.Replace("^", "");

                App.GS1Encoder.DataStr = val;
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
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
