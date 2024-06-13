using LabelVal.Sectors.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelVal.ImageRolls.Converters
{
    public class GS1TableNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GS1TableNames gs1Table)
            {
                switch (gs1Table)
                {
                    case GS1TableNames.None:
                        return "None";
                    case GS1TableNames.Unsupported:
                        return "";
                    case GS1TableNames._1:
                        return "1";
                    case GS1TableNames._1_8200:
                        return "1.8200";
                    case GS1TableNames._2:
                        return "2";
                    case GS1TableNames._3:
                        return "3";
                    case GS1TableNames._4:
                        return "4";
                    case GS1TableNames._5:
                        return "5";
                    case GS1TableNames._6:
                        return "6";
                    case GS1TableNames._7_1:
                        return "7.1";
                    case GS1TableNames._7_2:
                        return "7.2";
                    case GS1TableNames._7_3:
                        return "7.3";
                    case GS1TableNames._7_4:
                        return "7.4";
                    case GS1TableNames._8:
                        return "8";
                    case GS1TableNames._9:
                        return "9";
                    case GS1TableNames._10:
                        return "10";
                    case GS1TableNames._11:
                        return "11";
                    case GS1TableNames._12_1:
                        return "12.1";
                    case GS1TableNames._12_2:
                        return "12.2";
                    case GS1TableNames._12_3:
                        return "12.3";
                    default:
                        return string.Empty;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                switch (strValue)
                {
                    case "None":
                        return GS1TableNames.None;
                    case "":
                        return GS1TableNames.Unsupported;
                    case "1":
                        return GS1TableNames._1;
                    case "1.8200":
                        return GS1TableNames._1_8200;
                    case "2":
                        return GS1TableNames._2;
                    case "3":
                        return GS1TableNames._3;
                    case "4":
                        return GS1TableNames._4;
                    case "5":
                        return GS1TableNames._5;
                    case "6":
                        return GS1TableNames._6;
                    case "7.1":
                        return GS1TableNames._7_1;
                    case "7.2":
                        return GS1TableNames._7_2;
                    case "7.3":
                        return GS1TableNames._7_3;
                    case "7.4":
                        return GS1TableNames._7_4;
                    case "8":
                        return GS1TableNames._8;
                    case "9":
                        return GS1TableNames._9;
                    case "10":
                        return GS1TableNames._10;
                    case "11":
                        return GS1TableNames._11;
                    case "12.1":
                        return GS1TableNames._12_1;
                    case "12.2":
                        return GS1TableNames._12_2;
                    case "12.3":
                        return GS1TableNames._12_3;
                    default:
                        return GS1TableNames.None;
                }
            }

            return GS1TableNames.None;
        }
    }
}
