using System.Text.RegularExpressions;

namespace LabelVal.Models
{
    public class StandardEntryModel
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;

                Is300 = Name.EndsWith("300");
                IsGS1 = Name.ToLower().StartsWith("gs1");
                StandardName = name.Replace(" 300", "");

                if (IsGS1)
                {
                    var val = Regex.Match(Name, @"TABLE (\d*\.?\d+)");
                    if (val.Groups.Count == 2)
                        TableID = val.Groups[1].Value;
                }
            }
        }
        public string StandardPath { get; set; }

        public string StandardName { get; private set; }

        public string TableID { get; private set; }

        public bool Is300 { get; private set; }

        public bool IsGS1 { get; private set; }

    }
}
