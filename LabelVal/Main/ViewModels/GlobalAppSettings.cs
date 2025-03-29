using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Main.ViewModels
{
    public class  GlobalAppSettings
    {
        private static GlobalAppSettings _instance = new();
        public static GlobalAppSettings Instance => _instance;

        public bool ShowButtonText { get => App.Settings.GetValue("GlobalAppSettings_ShowButtonText", true, true); set => App.Settings.SetValue("GlobalAppSettings_ShowButtonText", value); }

    }
}
