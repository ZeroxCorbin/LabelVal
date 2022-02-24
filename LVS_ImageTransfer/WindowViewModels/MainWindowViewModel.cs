using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVS_ImageTransfer.WindowViewModele
{
    public class MainWindowViewModel:Core.BaseViewModel
    {
        public string ImageSourcePath { get => App.Settings.GetValue("ImageSourcePath", ""); set => App.Settings.SetValue("ImageSourcePath", value);  }

        public string FTPServerIP { get => App.Settings.GetValue("FTPServerIP", ""); set => App.Settings.SetValue("FTPServerIP", value); }
        public string FTPServerPath { get => App.Settings.GetValue("FTPServerPath", ""); set => App.Settings.SetValue("FTPServerPath", value); }
        public string FTPServerUserName { get => App.Settings.GetValue("FTPServerUserName", ""); set => App.Settings.SetValue("FTPServerUserName", value); }
        public string FTPServerPassword { get => App.Settings.GetValue("FTPServerPassword", ""); set => App.Settings.SetValue("FTPServerPassword", value); }

    }
}
