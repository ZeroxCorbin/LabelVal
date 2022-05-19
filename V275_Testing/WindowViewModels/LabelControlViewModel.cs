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
            set { if(value != isRun) App.Current.Dispatcher.Invoke(new Action(() => { ReadSectors.Clear(); IsStore = false; })); SetProperty(ref isRun, value); OnPropertyChanged("IsNotRun"); }
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

        public LabelControlViewModel(int labelNumber, string imagePath, string printerName, string gradingStandard, StandardsDatabase standardsDatabase, V275_API_Commands v275)
        {
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

        private void ClearStoredAction(object parameter)
        {
            StandardsDatabase.DeleteRow(GradingStandard, LabelNumber);
            GetStored();
        }
        private void ClearReadAction(object parameter)
        {
            ReadSectors.Clear();
            IsStore = false;
        }
        private void GetStored()
        {

            StoredSectors.Clear();

            StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, LabelNumber);

            if (row == null)
            {
                return;
            }

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

                            tempSectors.Add(new SectorControlViewModel(jSec, fSec , isWrongStandard));

                            break;
                        }
                    }
                }

            if (tempSectors.Count > 0)
            {
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    StoredSectors.Add(sec);
            }
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
            else
                return null;
        }
        private void LoadAction(object parameter) => _ = Load();

        public async Task<bool> Load()
        {
            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return false;
            }
            Job = V275.Job;

            foreach (var sec in Job.sectors)
            {
                await V275.DeleteSector(sec.name);

            }
            //StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, LabelNumber);

            //if (row == null)
            //{
            //    return;
            //}
            if(StoredSectors.Count > 0)
                if (!await V275.GetJob())
                {
                    Status = V275.Status;
                    return false;
                }
            foreach (var sec in StoredSectors)
            {
                await V275.AddSector(sec.JobSector.name, JsonConvert.SerializeObject(sec.JobSector));
            }

            return true;
        }

        private void InspectAction(object parameter) => _ = Read(-1);

        private void PrintAction(object parameter)
        {
            Task.Run(() =>
            {
                Printing?.Invoke(this);

                PrintControl printer = new PrintControl();
                printer.Print(ImagePath, PrintCount, PrinterName);
            });
        }
        public void ReadAction(object parameter) => _=Read(-1);

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

            if(repeat > 0)
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

                foreach(var sec in tempSectors)
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

        private void StoreAction(object parameter)
        {
            StandardsDatabase.AddRow(GradingStandard, LabelNumber, JsonConvert.SerializeObject(Job), JsonConvert.SerializeObject(Report));
            GetStored();
        }

        private string GetDisplayString(V275_Job job, V275_Report report)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in job.sectors)
            {
                sb.Append(item.username);

                string s = $"{item.gradingStandard.standard} TABLE {item.gradingStandard.tableId}";

                if (s != GradingStandard)
                {
                    sb.AppendLine(" WRONG GRADING STANDARD");
                }
                else
                {
                    sb.AppendLine();
                }
                //foreach (JObject rep in report.inspectLabel.inspectSector)
                //{
                //    //V275_Report_Verify1D verify1D;
                //    //V275_Report_Verify2D verify2D;
                //    if (item.name == rep["name"].ToString())
                //        foreach (JObject alm in rep["data"]["alarms"])
                //        {
                //            sb.Append('\t');
                //            sb.Append(alm["name"]);
                //            sb.Append(": ");
                //            sb.Append(alm["data"]["subAlarm"]);
                //            sb.Append(" ");
                //            sb.Append(alm["data"]["text"]);
                //            sb.Append(" / ");
                //            sb.Append(alm["data"]["expected"]);
                //            sb.AppendLine();
                //        }
                //    //if (type == "verify1D")
                //    //    verify1D = JsonConvert.DeserializeObject<V275_Report_Verify1D>(rep.ToString());
                //    //else if (type == "verify2D")
                //    //    verify2D = JsonConvert.DeserializeObject<V275_Report_Verify2D>(rep.ToString());


                //}
            }

            return sb.ToString();
        }
    }
}
