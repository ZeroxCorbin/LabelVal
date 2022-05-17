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
        public delegate void PrintingDelegate(string standard, int labelNumber);
        public event PrintingDelegate Printing;

        public string PrinterName { get; set; }
        public int RepeatNumber { get; }
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

        public int PrintCount { get; set; } = 3;

        V275_API_Commands V275 { get; }
        V275_Job Job { get; set; }
        V275_Report Report { get; set; }

        public ObservableCollection<SectorControlViewModel> ReadSectors { get; } = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> StoredSectors { get; } = new ObservableCollection<SectorControlViewModel>();

        public ObservableCollection<SectorControlViewModel> DiffSectors { get; } = new ObservableCollection<SectorControlViewModel>();

        public ICommand Print { get; }
        public ICommand Read { get; }
        public ICommand Store { get; }
        public ICommand Load { get; }
        public ICommand Inspect { get; }

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
            set { SetProperty(ref isRun, value); OnPropertyChanged("IsNotRun"); }
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

        public LabelControlViewModel(int repeatNumber, string imagePath, string printerName, string gradingStandard, StandardsDatabase standardsDatabase, V275_API_Commands v275)
        {
            RepeatNumber = repeatNumber;
            ImagePath = imagePath;
            PrinterName = printerName;
            GradingStandard = gradingStandard;
            StandardsDatabase = standardsDatabase;
            V275 = v275;

            Print = new Core.RelayCommand(PrintAction, c => true);
            Read = new Core.RelayCommand(ReadAction, c => true);
            Store = new Core.RelayCommand(StoreAction, c => true);
            Load = new Core.RelayCommand(LoadAction, c => true);
            Inspect = new Core.RelayCommand(InspectAction, c => true);

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
            StandardsDatabase.DeleteRow(GradingStandard, RepeatNumber);
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

            StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, RepeatNumber);

            if (row == null)
            {
                return;
            }

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
                            object fSec;
                            if (rSec["type"].ToString() == "verify1D")
                            {
                                fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify1D>(rSec.ToString());
                                StoredSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                            }
                            else if (rSec["type"].ToString() == "verify2D")
                            {
                                fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify2D>(rSec.ToString());
                                StoredSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                            }
                            else if (rSec["type"].ToString() == "ocr")
                            {
                                fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_OCR>(rSec.ToString());
                                StoredSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                            }
                            else if (rSec["type"].ToString() == "ocv")
                            {
                                fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_OCV>(rSec.ToString());
                                StoredSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                            }
                            break;
                        }
                    }
                }
        }

        private async void LoadAction(object parameter)
        {
            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return;
            }
            Job = V275.Job;

            foreach (var sec in Job.sectors)
            {
                await V275.DeleteSector(sec.name);

            }
            //StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, RepeatNumber);

            //if (row == null)
            //{
            //    return;
            //}
            foreach (var sec in StoredSectors)
            {
                await V275.AddSector(sec.JobSector.name, JsonConvert.SerializeObject(sec.JobSector));
            }
        }

        private void InspectAction(object parameter)
        {
            ReadAction(parameter);
        }

        private void PrintAction(object parameter)
        {
            Task.Run(() =>
            {
                PrintControl printer = new PrintControl();
                printer.Print(ImagePath, PrintCount, PrinterName);
            });
        }

        private async void ReadAction(object parameter)
        {
            Status = string.Empty;

            ReadSectors.Clear();
            IsStore = false;

            if (!await V275.GetJob())
            {
                Status = V275.Status;
                return;
            }
            Job = V275.Job;

            if (!await V275.Inspect())
            {
                Status = V275.Status;
                return;
            }

            Thread.Sleep(1000);

            if (!await V275.GetReport())
            {
                Status = V275.Status;
                return;
            }
            Report = V275.Report;

            //foreach (var jSec in Job.sectors)
            //    foreach (V275_Report_InspectSector rSec in Report.inspectLabel.inspectSector)
            //        if (jSec.name == rSec.name)
            //        {
            //            ReadSectors.Add(new SectorControlViewModel(jSec, rSec));
            //            break;
            //        }

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
                        object fSec;
                        if (rSec["type"].ToString() == "verify1D")
                        {
                            fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify1D>(rSec.ToString());
                            ReadSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                        }
                        else if (rSec["type"].ToString() == "verify2D")
                        {
                            fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify2D>(rSec.ToString());
                            ReadSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                        }
                        else if (rSec["type"].ToString() == "ocr")
                        {
                            fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_OCR>(rSec.ToString());
                            ReadSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                        }
                        else if (rSec["type"].ToString() == "ocv")
                        {
                            fSec = JsonConvert.DeserializeObject<V275_Report_InspectSector_OCV>(rSec.ToString());
                            ReadSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));
                        }

                        break;
                    }
                }
            }

            if (ReadSectors.Count > 0)
                IsStore = true;

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
        }

        private void StoreAction(object parameter)
        {
            StandardsDatabase.AddRow(GradingStandard, RepeatNumber, JsonConvert.SerializeObject(Job), JsonConvert.SerializeObject(Report));
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
