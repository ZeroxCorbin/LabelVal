using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using V275_Testing.Databases;
using V275_Testing.Printer;
using V275_Testing.V275;
using V275_Testing.V275.Models;

namespace V275_Testing.WindowViewModels
{
    public class LabelControlViewModel : Core.BaseViewModel
    {
        public delegate void PrintingDelegate(LabelControlViewModel label);
        public event PrintingDelegate Printing;

        public string PrinterName { get; set; }
        public int LabelNumber { get; }
        public BitmapImage RepeatImage { get; } = new BitmapImage();

        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        private string _Status;

        public string GradingStandard { get; }
        private StandardsDatabase StandardsDatabase { get; }

        private string ImagePath { get; }

        public int PrintCount { get; set; } = 1;

        V275_API_Commands V275 { get; }
        V275_Job Job { get; set; }
        V275_Report Report { get; set; }

        public ObservableCollection<SectorControlViewModel> ReadSectors { get; } = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> StoredSectors { get; } = new ObservableCollection<SectorControlViewModel>();

        public ObservableCollection<SectorControlViewModel> DiffSectors { get; } = new ObservableCollection<SectorControlViewModel>();

        public ICommand PrintCommand { get; }
        public ICommand ReadCommand { get; }
        public ICommand StoreCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand InspectCommand { get; }

        public ICommand ClearStored { get; }
        public ICommand ClearRead { get; }

        public bool IsSetup
        {
            get => isSetup;
            set { SetProperty(ref isSetup, value); OnPropertyChanged("IsNotSetup"); }
        }
        public bool IsNotSetup => !isSetup;
        private bool isSetup = false;

        public bool IsRun
        {
            get => isRun;
            set { /*if (value != isRun) App.Current.Dispatcher.Invoke(new Action(() => { ReadSectors.Clear(); IsStore = false; }));*/ SetProperty(ref isRun, value); OnPropertyChanged("IsNotRun"); }
        }
        public bool IsNotRun => !isRun;
        private bool isRun = false;

        public bool IsStore
        {
            get => isStore;
            set { SetProperty(ref isStore, value); OnPropertyChanged("IsNotStore"); }
        }
        public bool IsNotStore => !isStore;
        private bool isStore = false;

        public bool IsLoad
        {
            get => isLoad;
            set { SetProperty(ref isLoad, value); OnPropertyChanged("IsNotLoad"); }
        }
        public bool IsNotLoad => !isLoad;
        private bool isLoad = false;

        public bool IsWorking
        {
            get => isWorking;
            set { SetProperty(ref isWorking, value); OnPropertyChanged("IsNotWorking"); }
        }
        public bool IsNotWorking => !isWorking;
        private bool isWorking = false;

        private IDialogCoordinator dialogCoordinator;
        public LabelControlViewModel(int labelNumber, string imagePath, string printerName, string gradingStandard, StandardsDatabase standardsDatabase, V275_API_Commands v275, IDialogCoordinator diag)
        {
            dialogCoordinator = diag;

            LabelNumber = labelNumber;
            ImagePath = imagePath;
            PrinterName = printerName;
            GradingStandard = gradingStandard;
            StandardsDatabase = standardsDatabase;
            V275 = v275;

            PrintCommand = new Core.RelayCommand(PrintAction, c => true);
            ReadCommand = new Core.RelayCommand(ReadAction, c => true);
            StoreCommand = new Core.RelayCommand(StoreAction, c => true);
            LoadCommand = new Core.RelayCommand(LoadAction, c => true);
            InspectCommand = new Core.RelayCommand(InspectAction, c => true);

            ClearStored = new Core.RelayCommand(ClearStoredAction, c => true);
            ClearRead = new Core.RelayCommand(ClearReadAction, c => true);

            GetImage(imagePath);
            GetStored();
        }

        public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
        {
            MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

            return result;
        }

        private void GetImage(string imagePath)
        {
            RepeatImage.BeginInit();
            RepeatImage.UriSource = new Uri(imagePath);

            // To save significant application memory, set the DecodePixelWidth or
            // DecodePixelHeight of the BitmapImage value of the image source to the desired
            // height or width of the rendered image. If you don't do this, the application will
            // cache the image as though it were rendered as its normal size rather then just
            // the size that is displayed.
            // Note: In order to preserve aspect ratio, set DecodePixelWidth
            // or DecodePixelHeight but not both.
            //RepeatImage.DecodePixelWidth = 200;
            RepeatImage.EndInit();
        }

        private void PrintAction(object parameter)
        {
            IsWorking = true;
            Task.Run(() =>
            {
                Printing?.Invoke(this);

                PrintControl printer = new PrintControl();
                printer.Print(ImagePath, PrintCount, PrinterName);

                if (!IsRun)
                    IsWorking = false;
            });
        }

        private void GetStored()
        {
            StoredSectors.Clear();
            IsLoad = false;

            StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, LabelNumber);

            if (row == null)
                return;

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            if (!string.IsNullOrEmpty(row.Report) && !string.IsNullOrEmpty(row.Job))
                foreach (var jSec in JsonConvert.DeserializeObject<V275_Job>(row.Job).sectors)
                {
                    bool isWrongStandard = false;
                    if (jSec.type == "verify1D" || jSec.type == "verify2D")
                        if (jSec.gradingStandard.enabled)
                            isWrongStandard = !(GradingStandard == $"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}");
                        else
                            isWrongStandard = true;

                    foreach (JObject rSec in JsonConvert.DeserializeObject<V275_Report>(row.Report).inspectLabel.inspectSector)
                    {
                        if (jSec.name == rSec["name"].ToString())
                        {

                            object fSec = DeserializeSector(rSec);

                            if (fSec == null)
                                break;

                            tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));

                            break;
                        }
                    }
                }

            if (tempSectors.Count > 0)
            {
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    StoredSectors.Add(sec);

                IsLoad = true;
            }
        }
        private async void StoreAction(object parameter)
        {
            if (StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for label {LabelNumber}?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            StandardsDatabase.AddRow(GradingStandard, LabelNumber, JsonConvert.SerializeObject(Job), JsonConvert.SerializeObject(Report));
            GetStored();
        }
        private async void ClearStoredAction(object parameter)
        {
            if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for label {LabelNumber}?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
            {
                StandardsDatabase.DeleteRow(GradingStandard, LabelNumber);
                GetStored();
            }

        }

        private void LoadAction(object parameter) => _ = Load();
        public async Task<int> Load()
        {
            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return -1;
            }
            Job = V275.Job;

            foreach (var sec in Job.sectors)
                await V275.DeleteSector(sec.name);

            await V275.Inspect();
            await V275.GetReport();

            if (StoredSectors.Count == 0)
            {
                if (!await V275.GetDetect())
                {
                    Status = V275.Status;
                    return -1;
                }
                if (!await V275.Detect())
                {
                    Status = V275.Status;
                    return -1;
                }
                return 2;
            }

            foreach (var sec in StoredSectors)
                await V275.AddSector(sec.JobSector.name, JsonConvert.SerializeObject(sec.JobSector));

            return 1;
        }

        private void InspectAction(object parameter) => _ = Read(-1);
        public void ReadAction(object parameter) => _ = Read(-1);
        public async Task<bool> Read(int repeat)
        {
            Status = string.Empty;

            ReadSectors.Clear();
            IsStore = false;

            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return false;
            }
            Job = V275.Job;

            if (repeat > 0)
                if (!await V275.SetRepeat(repeat))
                {
                    Status = V275.Status;
                    return false;
                }

            if (!await V275.Inspect())
            {
                Status = V275.Status;
                return false;
            }

            Thread.Sleep(1000);

            if (!await V275.GetReport())
            {
                Status = V275.Status;
                return false;
            }
            Report = V275.Report;

            //foreach (var jSec in Job.sectors)
            //    foreach (V275_Report_InspectSector rSec in Report.inspectLabel.inspectSector)
            //        if (jSec.name == rSec.name)
            //        {
            //            ReadSectors.Add(new SectorControlViewModel(jSec, rSec));
            //            break;
            //        }

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            foreach (var jSec in Job.sectors)
            {
                bool isWrongStandard = false;
                if (jSec.type == "verify1D" || jSec.type == "verify2D")
                    if (jSec.gradingStandard.enabled)
                        isWrongStandard = !(GradingStandard == $"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}");
                    else
                        isWrongStandard = true;

                foreach (JObject rSec in Report.inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {
                        object fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break; //Not yet supported sector type

                        tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));

                        break;
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                IsStore = true;
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    ReadSectors.Add(sec);
            }


            List<V275_Report_InspectSector_Compare> diff = new List<V275_Report_InspectSector_Compare>();
            foreach (var sec in StoredSectors)
            {
                bool found = false;
                foreach (var cSec in ReadSectors)
                    if (sec.JobSector.name == cSec.JobSector.name)
                    {
                        found = true;
                        diff.Add(sec.CompareSector.Compare(cSec.CompareSector));
                    }

                if (!found)
                {
                    var dat = sec.CompareSector.Compare(new V275_Report_InspectSector_Compare());
                    dat.IsSectorMissing = true;
                    diff.Add(dat);
                }

            }

            return true;
        }
        private void ClearReadAction(object parameter)
        {
            ReadSectors.Clear();
            IsStore = false;
        }

        public async Task<bool> CreateSectors(V275_Events_System ev, string gradingStandard)
        {
            int d1 = 1;
            int d2 = 1;
            bool res = true;

            V275_Job_Sector_Verify verify = new V275_Job_Sector_Verify();
            foreach (var val in ev.data.detections)
            {
                bool isGS1 = false;
                if (gradingStandard.StartsWith("GS1"))
                    isGS1 = true;

                string table = "1";
                if (isGS1)
                    table = gradingStandard.Replace("GS1 TABLE ", "");

                V275_Symbologies.Symbol sym = V275.Symbologies.Find((e) => e.symbology == val.symbology);
                if (sym == null)
                    continue;

                if (sym.regionType == "verify1D")
                    verify.id = d1++;
                else
                    verify.id = d2++;

                verify.type = sym.regionType;
                verify.symbology = val.symbology;
                verify.name = $"{sym.regionType}_{verify.id}";
                verify.username = $"{char.ToUpper(verify.name[0])}{verify.name.Substring(1)}";

                verify.top = val.region.y;
                verify.left = val.region.x;
                verify.height = val.region.height;
                verify.width = val.region.width;

                verify.orientation = val.orientation;

                verify.gradingStandard.enabled = isGS1;
                verify.gradingStandard.tableId = table;

                res &= await V275.AddSector(verify.name, JsonConvert.SerializeObject(verify));
            }

            return res;
        }
        private object DeserializeSector(JObject reportSec)
        {
            if (reportSec["type"].ToString() == "verify1D")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify1D>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "verify2D")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify2D>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "ocr")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_OCR>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "ocv")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_OCV>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "blemish")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Blemish>(reportSec.ToString());
            }
            else
                return null;
        }
    }
}
