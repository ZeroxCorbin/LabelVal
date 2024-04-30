using NHibernate.Linq.Functions;
using System.Text.RegularExpressions;

namespace LabelVal.Models
{
    public class StandardEntryModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string DPI { get; private set; }


        public string TableID { get; private set; }
        public bool IsGS1 { get; private set; }


        public StandardEntryModel(string name, string path)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
                return;

            Name = name;
            Path = path;

            DPI = Path[(Path.LastIndexOf('\\') + 1)..];

            IsGS1 = Name.StartsWith("gs1", System.StringComparison.CurrentCultureIgnoreCase);

            if (IsGS1)
            {
                var val = Regex.Match(Name, @"TABLE (\d*\.?\d+)");

                if (val.Groups.Count == 2)
                    TableID = val.Groups[1].Value;
            }
        }
    }
}
