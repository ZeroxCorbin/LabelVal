using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LabelVal.V275.Converters
{
    public class V275_OCMatchModeToString : IValueConverter
    {
        //public enum OCRMatchModes
        //{
        //    Standard = 0,
        //    MatchRegion = 2,
        //    SequentialInc = 3,
        //    SequentialDec = 4,
        //    MatchAtStart = 5,
        //    FileAtStart = 6,
        //    DuplicateCheck = 7,
        //}
        private Dictionary<int, string> MatchModes { get; } = new Dictionary<int, string>()
        {
            {0, "Standard" },
            {1, "Exact String" },
            {2, "Match Region" },
            {3, "Sequential Inc+" },
            {4, "Sequential Dec-" },
            {5, "Match Start" },
            {6, "File Start" },
            {7, "Duplicate Check" },

        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return value;

            if(MatchModes.ContainsKey((int)value))
                return MatchModes[(int)value];
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

