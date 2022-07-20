using LabelVal.WindowViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LabelVal.LVS_95xx
{
    internal class LVS95xx_SerialPortViewModel : Core.BaseViewModel
    {
        private double width;
        public double Width { get => width; set => SetProperty(ref width, value); }

        private double height;
        public double Height { get => height; set => SetProperty(ref height, value); }

        private string readData;
        public string ReadData { get => readData; set => SetProperty(ref readData, value); }

        private ObservableCollection<string> comNameList = new ObservableCollection<string>();
        public ObservableCollection<string> ComNameList { get => comNameList; set => SetProperty(ref comNameList, value); }

        public string SelectedComName { get => App.Settings.GetValue("95xx_COM_Name", ""); set { App.Settings.SetValue("95xx_COM_Name", value); } }

        public SectorControlViewModel StoredSectorControl { get; }

        private SectorControlViewModel lvs95xxSectorControl;
        public SectorControlViewModel Lvs95xxSectorControl { get => lvs95xxSectorControl; set => SetProperty(ref lvs95xxSectorControl, value); }


        public ICommand WriteData { get; }
        public ICommand OpenPort { get; }
        public ICommand ClosePort { get; }

        LVS95xx_SerialPort PortController = new LVS95xx_SerialPort();

        public LVS95xx_SerialPortViewModel(object sectorControl)
        {
            StoredSectorControl = (SectorControlViewModel)sectorControl;

            WriteData = new Core.RelayCommand(WriteDataAction, a => true);
            ClosePort = new Core.RelayCommand(ClosePortAction, a => true);
            OpenPort = new Core.RelayCommand(OpenPortAction, a => true);
            //lVS95Xx_SerialPortSetup.DataAvailable += LVS95Xx_SerialPortSetup_DataAvailable;
            //Task.Run(() => lVS95Xx_SerialPortSetup.StartCancellableProcess("cmd.exe", "", new System.Threading.CancellationToken()));

            LoadComList();
        }

        private void LoadComList()
        {
            ComNameList.Clear();
            PortController.Controller.GetCOMList();

            foreach (var com in PortController.Controller.COMPortsAvailable)
                ComNameList.Add(com);
        }
 
        public void OpenPortAction(object parameter)
        {
            if (!string.IsNullOrEmpty(SelectedComName))
            {
                PortController.Init(SelectedComName);

                PortController.SectorAvailable -= PortController_SectorAvailable;
                PortController.SectorAvailable += PortController_SectorAvailable;

                PortController.PacketAvailable -= PortController_PacketAvailable;
                PortController.PacketAvailable += PortController_PacketAvailable;

                PortController.Start();
            }

        }

        private void PortController_SectorAvailable(object sector)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            Lvs95xxSectorControl = new SectorControlViewModel(StoredSectorControl.JobSector, sector, false, true)));
        }

        private void PortController_PacketAvailable(string packet)
        {
            ReadData = packet;
        }

        public void ClosePortAction(object parameter)
        {
            PortController.SectorAvailable -= PortController_SectorAvailable;
            PortController.PacketAvailable -= PortController_PacketAvailable;

            PortController.Stop();
        }
        public void WriteDataAction(object parameter)
        {
            //lVS95Xx_SerialPortSetup.Write(@"cd D:\OneDrive - OMRON\Omron\OCR\Applications\LabelVal\LabelVal\bin\Debug\LVS-95xx\com0com\x64" + "\r\n");
            //lVS95Xx_SerialPortSetup.Write(".\\setupc.exe");
            //lVS95Xx_SerialPortSetup.Write("\r\n");
        }
    }
}
