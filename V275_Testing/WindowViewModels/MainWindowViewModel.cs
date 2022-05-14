using V275_Testing.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using V275_Testing.V275;
using V275_Testing.V275.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using V275_Testing.Databases;

namespace V275_Testing.WindowViewModels
{
    public class MainWindowViewModel : Core.BaseViewModel
    {
        private V275_API_Commands V275 = new V275_API_Commands();
        private StandardsDatabase StandardsDatabase { get; }

        public string V275_Host { get => V275.Host = App.Settings.GetValue("V275_Host", "127.0.0.1"); set { App.Settings.SetValue("V275_Host", value); V275.Host = value; } }
        public string V275_SystemPort { get => V275.SystemPort = App.Settings.GetValue("V275_SystemPort", "8080"); set { App.Settings.SetValue("V275_SystemPort", value); ; V275.SystemPort = value; } }
        public string V275_NodeNumber { get => V275.NodeNumber = App.Settings.GetValue("V275_NodeNumber", "1"); set { App.Settings.SetValue("V275_NodeNumber", value); V275.NodeNumber = value; } }

        public ObservableCollection<V275_Devices.Node> Nodes { get; } = new ObservableCollection<V275_Devices.Node>();
        public V275_Devices.Node SelectedNode { get => selectedNode; set { SetProperty(ref selectedNode, value); if (value != null) V275_NodeNumber = value.enumeration.ToString(); } }
        private V275_Devices.Node selectedNode;

        public ObservableCollection<RepeatControlViewModel> Repeats { get; } = new ObservableCollection<RepeatControlViewModel>();

        public string StoredStandard { get => App.Settings.GetValue("StoredStandard", "GS1 Table 1"); set { App.Settings.SetValue("StoredStandard", value); } }
        public ObservableCollection<string> Standards { get; } = new ObservableCollection<string>();
        public string SelectedStandard
        {
            get => selectedStandard;
            set
            {
                SetProperty(ref selectedStandard, value);

                if (!string.IsNullOrEmpty(value))
                {
                    StoredStandard = value;
                    LoadRepeats();
                }
                else
                {
                    Repeats.Clear();
                }
            }
        }
        private string selectedStandard;

        public string StoredPrinter { get => App.Settings.GetValue("StoredPrinter", ""); set { App.Settings.SetValue("StoredPrinter", value); } }
        public ObservableCollection<string> Printers { get; } = new ObservableCollection<string>();
        public string SelectedPrinter
        {
            get => selectedPrinter;
            set
            {
                SetProperty(ref selectedPrinter, value);

                if (!string.IsNullOrEmpty(value))
                {
                    StoredPrinter = value;

                    UpdatePrinters();
                }
            }
        }
        private string selectedPrinter;

        public string UserName { get => App.Settings.GetValue("UserName", "admin"); set => App.Settings.SetValue("UserName", value); }
        public string Password { get => App.Settings.GetValue("Password", "admin"); set => App.Settings.SetValue("Password", value); }

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        private string _Status;

        public ICommand GetDevices { get; }
        public bool IsGetDevices
        {
            get => isGetDevices;
            set { SetProperty(ref isGetDevices, value); OnPropertyChanged("IsNotGetDevices"); }
        }
        public bool IsNotGetDevices => !isGetDevices;
        private bool isGetDevices = false;

        public ICommand LoginMonitor { get; }
        public ICommand LoginControl { get; }
        public ICommand Logout { get; }
        public bool IsLogggedIn
        {
            get => isLoggedIn;
            set { SetProperty(ref isLoggedIn, value); OnPropertyChanged("IsNotLogggedIn"); }
        }
        public bool IsNotLogggedIn => !isLoggedIn;
        private bool isLoggedIn = false;

        public ICommand Start { get; }
        public bool IsStarted
        {
            get => isStarted;
            set { SetProperty(ref isStarted, value); OnPropertyChanged("IsNotStarted"); }
        }
        public bool IsNotStarted => !isStarted;
        private bool isStarted = false;


        public ICommand Stop { get; }


        public MainWindowViewModel()
        {
            GetDevices = new Core.RelayCommand(GetDevicesAction, c => true);
            LoginMonitor = new Core.RelayCommand(LoginMonitorAction, c => true);
            LoginControl = new Core.RelayCommand(LoginControlAction, c => true);
            Logout = new Core.RelayCommand(LogoutAction, c => true);
            Start = new Core.RelayCommand(StartAction, c => true);
            Stop = new Core.RelayCommand(StopAction, c => true);

            StandardsDatabase = new StandardsDatabase($"{App.UserDataDirectory}\\{App.StandardsDatabaseName}");

            LoadPrinters();
            SetupGradingStandards();
        }

        private void LoadPrinters()
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                Printers.Add(p);

                if (StoredPrinter == p)
                    SelectedPrinter = p;
            }

            if (string.IsNullOrEmpty(SelectedPrinter) && Printers.Count > 0)
                SelectedPrinter = Printers.First();
        }

        private void UpdatePrinters()
        {
            foreach (var r in Repeats)
                r.PrinterName = StoredPrinter;

        }

        private void LoadRepeats()
        {
            List<string> Images = new List<string>();
            Images.Clear();
            foreach (var f in Directory.EnumerateFiles($"{App.StandardsRoot}\\{StoredStandard}\\600\\"))
                Images.Add(f);

            Images.Sort();

            int i = 1;
            foreach (var img in Images)
            {
                Repeats.Add(new RepeatControlViewModel(i++, img, SelectedPrinter, SelectedStandard, StandardsDatabase, V275));
            }
        }

        private void Reset()
        {
            Status = "";
        }

        private async void GetDevicesAction(object parameter)
        {
            Reset();

            Nodes.Clear();
            SelectedNode = null;

            if (await V275.GetDevices())
            {
                foreach (var node in V275.Devices.nodes)
                {
                    Nodes.Add(node);
                    if (node.enumeration.ToString() == V275_NodeNumber)
                        SelectedNode = node;
                }

                if (SelectedNode == null && Nodes.Count > 0)
                    SelectedNode = Nodes.First();

                IsGetDevices = true;
            }
            else
            {
                Status = V275.Status;
                IsGetDevices = false;
            }

        }

        private void SetupGradingStandards()
        {
            Reset();

            Standards.Clear();
            SelectedStandard = null;

            foreach (var dir in Directory.EnumerateDirectories(App.StandardsRoot))
            {
                Standards.Add(dir.Substring(dir.LastIndexOf("\\") + 1));

            }

            List<string> tables = StandardsDatabase.GetAllTables();

            foreach (var standard in Standards)
            {
                if (tables.Contains(standard))
                    continue;

                StandardsDatabase.CreateTable(standard);
            }

            if (Standards.Contains(StoredStandard))
                SelectedStandard = StoredStandard;
            else if (Standards.Count > 0)
                SelectedStandard = Standards.First();

            //if (await V275.GetGradingStandards())
            //{
            //    foreach (var standard in V275.GradingStandards.gradingStandards)
            //    {
            //        string s = $"{standard.standard} Table {standard.tableId}";

            //        if (!Standards.Contains(s))
            //        {
            //            if (Directory.Exists($"{App.StandardsRoot}\\{s}"))
            //                Standards.Add(s);
            //            else
            //                s = "";
            //        }
            //        else
            //            continue;

            //        if (StoredStandard == s)
            //            SelectedStandard = StoredStandard;
            //    }

            //    if (SelectedStandard == null && Standards.Count > 0)
            //        SelectedStandard = Standards.First();
            //}
            //else
            //{
            //    Status = V275.Status;
            //}
        }

        private async void LoginMonitorAction(object parameter)
        {
            Reset();

            if (await V275.Login(UserName, Password, true))
            {
                IsLogggedIn = true;

                foreach (var rep in Repeats)
                {
                    rep.IsSetup = true;
                }
            }
            else
            {
                Status = V275.Status;
                IsLogggedIn = false;
            }
        }

        private async void LoginControlAction(object parameter)
        {
            Reset();

            if (await V275.Login(UserName, Password, false))
            {
                IsLogggedIn = true;

                foreach (var rep in Repeats)
                {
                    rep.IsRun = true;
                }
            }
            else
            {
                Status = V275.Status;
                IsLogggedIn = false;
            }
        }

        private async void LogoutAction(object parameter)
        {
            Reset();

            if (await V275.Logout())
                IsLogggedIn = false;
            else
            {
                Status = V275.Status;
                IsLogggedIn = false;
            }

            foreach (var rep in Repeats)
            {
                rep.IsRun = false;
                rep.IsSetup = false;
            }

        }

        private async void StartAction(object parameter)
        {
            Reset();
            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return;
            }
            if (!await V275.GetReport())
            {
                Status = V275.Status;
                return;
            }

            //  Printer.PrintControl.Print(System.IO.Directory.GetCurrentDirectory() + "\\Assets\\GS1\\Table 1\\600\\", 1);

            //bool res = true;
            //string result = await V275_API_Connection.Put(V275_API_URLs.Print(), V275_API_URLs.Print_Body(true), Token);
            //Thread.Sleep(200);
            //result = await V275_API_Connection.Put(V275_API_URLs.Print(), V275_API_URLs.Print_Body(false), Token);
            //result = await V275_API_Connection.Put(V275_API_URLs.History("2"), "", Token);
            //string result1 = await V275_API_Connection.Put(V275_API_URLs.Inspect(), "", Token);

            //Thread.Sleep(1000);
            //string report = await V275_API_Connection.Get(V275_API_URLs.Report(), Token);

            //if (string.IsNullOrEmpty(report))
            //{
            //    res = false;
            //    Status = "No data returned!";
            //}
            //if (V275_API_Connection.IsException)
            //{
            //    res = false;
            //    Status = V275_API_Connection.Exception.Message;
            //}

            //if (res)
            //{
            //    V275_Report job = JsonConvert.DeserializeObject<V275_Report>(report);

            //}
        }



        private void StopAction(object parameter)
        {

        }

    }
}
