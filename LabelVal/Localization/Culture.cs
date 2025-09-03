using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Localization
{
    public static class Culture
    {
        public static string GetCulture(string language)=> language switch
            {
                "English" => "en-US",
                "Español" => "es-MX",
                _ => "en-US",
            };
        
        public static string GetLanguage(string culture) => culture switch
            {
                "en-US" => "English",
                "es-MX" => "Español",
                _ => "English",
            };
    }
}
