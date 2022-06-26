using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using V275_Testing.Databases;
using V275_Testing.Printer;
using V275_Testing.Utilities;
using V275_Testing.V275;
using V275_Testing.V275.Models;

namespace V275_Testing.WindowViewModels
{
    public class LabelControlViewModel : Core.BaseViewModel
    {
        public delegate void PrintingDelegate(LabelControlViewModel label);
        public event PrintingDelegate Printing;

        public delegate void BringIntoViewDelegate();
        public event BringIntoViewDelegate BringIntoView;

        private string labelImageUID;
        public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

        private string labelComment;
        public string LabelComment { get => labelComment; set => SetProperty(ref labelComment, value); }

        private byte[] labelImage;
        public byte[] LabelImage { get => labelImage; set => SetProperty(ref labelImage, value); }

        public V275_Job LabelTemplate { get; set; }

        private byte[] repeatImage = null;
        public byte[] RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

        private bool isGS1Standard;
        public bool IsGS1Standard { get => isGS1Standard; set => SetProperty(ref isGS1Standard, value); }

        private string gradingStandard;
        public string GradingStandard { get => gradingStandard; set => SetProperty(ref gradingStandard, value); }

        private bool isGoldenRepeat;
        public bool IsGoldenRepeat { get => isGoldenRepeat; set => SetProperty(ref isGoldenRepeat, value); }

        private int printCount = 1;
        public int PrintCount { get => printCount; set => SetProperty(ref printCount, value); }



        private ObservableCollection<SectorControlViewModel> labelSectors = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> LabelSectors { get => labelSectors; set => SetProperty(ref labelSectors, value); }

        private ObservableCollection<SectorControlViewModel> repeatSectors = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> RepeatSectors { get => repeatSectors; set => SetProperty(ref repeatSectors, value); }


        public bool IsLoggedIn_Monitor
        {
            get => isLoggedIn_Monitor;
            set { SetProperty(ref isLoggedIn_Monitor, value); OnPropertyChanged("IsNotLoggedIn_Monitor"); }
        }
        public bool IsNotLoggedIn_Monitor => !isLoggedIn_Monitor;
        private bool isLoggedIn_Monitor = false;

        public bool IsLoggedIn_Control
        {
            get => isLoggedIn_Control;
            set
            {
                SetProperty(ref isLoggedIn_Control, value);
                OnPropertyChanged("IsNotLoggedIn_Control");
                if (value) PrintCount = 1;
            }
        }
        public bool IsNotLoggedIn_Control => !isLoggedIn_Control;
        private bool isLoggedIn_Control = false;

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


        public bool IsFaulted
        {
            get => isFaulted;
            set { SetProperty(ref isFaulted, value); OnPropertyChanged("IsNotFaulted"); }
        }
        public bool IsNotFaulted => !isFaulted;
        private bool isFaulted = false;


        public string PrinterName { get; set; }
        public string LabelImagePath { get; }



        public string Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        private string _Status;


        private StandardsDatabase StandardsDatabase { get; set; }

        public V275_API_Controller V275 { get; private set; }
        public V275_Job ReadJob { get; set; }
        //public string StoredJob { get; private set; }
        public V275_Report Report { get; private set; }

        public ICommand PrintCommand { get; }
        public ICommand ReadCommand { get; }
        public ICommand StoreCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand InspectCommand { get; }

        public ICommand ClearStored { get; }
        public ICommand ClearRead { get; }



        private IDialogCoordinator dialogCoordinator;
        public LabelControlViewModel(string imagePath, string imageComment, string printerName, string gradingStandard, StandardsDatabase standardsDatabase, V275_API_Controller v275, IDialogCoordinator diag)
        {
            dialogCoordinator = diag;

            LabelImagePath = imagePath;
            LabelComment = imageComment;
            PrinterName = printerName;
            GradingStandard = gradingStandard;
            StandardsDatabase = standardsDatabase;
            V275 = v275;

            IsGS1Standard = GradingStandard.StartsWith("GS1") ? true : false;

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
            LabelImage = File.ReadAllBytes(imagePath);
            LabelImageUID = ImageUtilities.ImageUID(LabelImage);
            //LabelImage.BeginInit();
            //LabelImage.UriSource = new Uri(imagePath);

            //// To save significant application memory, set the DecodePixelWidth or
            //// DecodePixelHeight of the BitmapImage value of the image source to the desired
            //// height or width of the rendered image. If you don't do this, the application will
            //// cache the image as though it were rendered as its normal size rather then just
            //// the size that is displayed.
            //// Note: In order to preserve aspect ratio, set DecodePixelWidth
            //// or DecodePixelHeight but not both.
            //LabelImage.DecodePixelHeight = 400;
            //LabelImage.EndInit();
            //LabelImage.Freeze();
        }

        public void PrintAction(object parameter)
        {
            IsWorking = true;
            IsFaulted = false;

            BringIntoView?.Invoke();
            Printing?.Invoke(this);
        }

        private void GetStored()
        {
            foreach(var sec in LabelSectors)
                sec.Clear();

            LabelSectors.Clear();
            IsLoad = false;

            StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, LabelImageUID);

            if (row == null)
            {
                RepeatImage = null;
                IsGoldenRepeat = false;
                return;
            }

            LabelTemplate = JsonConvert.DeserializeObject<V275_Job>(row.LabelTemplate);
            RepeatImage = row.RepeatImage;
            IsGoldenRepeat = true;

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            if (!string.IsNullOrEmpty(row.LabelReport) && !string.IsNullOrEmpty(row.LabelTemplate))
                foreach (var jSec in LabelTemplate.sectors)
                {
                    bool isWrongStandard = false;
                    if (jSec.type == "verify1D" || jSec.type == "verify2D")
                        if (jSec.gradingStandard.enabled && IsGS1Standard)
                            isWrongStandard = !(GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));
                        else
                            isWrongStandard = false;

                    foreach (JObject rSec in JsonConvert.DeserializeObject<V275_Report>(row.LabelReport).inspectLabel.inspectSector)
                    {
                        if (jSec.name == rSec["name"].ToString())
                        {

                            object fSec = DeserializeSector(rSec);

                            if (fSec == null)
                                break;

                            tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard, jSec.gradingStandard == null ? false : jSec.gradingStandard.enabled));

                            break;
                        }
                    }
                }

            if (tempSectors.Count > 0)
            {
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    LabelSectors.Add(sec);

                IsLoad = true;
            }
        }
        private async void StoreAction(object parameter)
        {
            if (LabelSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this label?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            StandardsDatabase.AddRow(GradingStandard, LabelImageUID, JsonConvert.SerializeObject(ReadJob), JsonConvert.SerializeObject(Report), RepeatImage);
            GetStored();
        }
        private async void ClearStoredAction(object parameter)
        {
            if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this label?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
            {
                StandardsDatabase.DeleteRow(GradingStandard, LabelImageUID);
                GetStored();
            }
        }

        private void LoadAction(object parameter) => _ = Load();
        public async Task<int> Load()
        {
            if (!await V275.DeleteSectors())
            {
                Status = V275.Status;
                return -1;
            }

            if (LabelSectors.Count == 0)
            {
                if (!await V275.DetectSectors())
                {
                    Status = V275.Status;
                    return -1;
                }

                return 2;
            }

            foreach (var sec in LabelSectors)
            {
                if (!await V275.AddSector(sec.JobSector.name, JsonConvert.SerializeObject(sec.JobSector)))
                {
                    Status = V275.Status;
                    return -1;
                }
            }

            return 1;
        }

        private void InspectAction(object parameter) => _ = Read(0);
        public void ReadAction(object parameter) => _ = Read(0);
        public async Task<bool> Read(int repeat, bool isRunning = false)
        {
            Status = string.Empty;

            //if (!isRunning)
            //{
            RepeatSectors.Clear();
            ReadJob = null;
            //RepeatImage = null;
            IsStore = false;
            //}

            if (!await V275.Read(repeat))
            {
                Status = V275.Status;
                return false;
            }

            ReadJob = V275.Commands.Job;
            Report = V275.Commands.Report;
            RepeatImage = V275.Commands.Repeatimage;
            IsGoldenRepeat = false;
             
            //if (!isRunning)
            //{
            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            foreach (var jSec in ReadJob.sectors)
            {
                bool isWrongStandard = false;
                if (jSec.type == "verify1D" || jSec.type == "verify2D")
                    if (jSec.gradingStandard.enabled && IsGS1Standard)
                        isWrongStandard = !(GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));
                    else
                        isWrongStandard = false;

                foreach (JObject rSec in Report.inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {
                        object fSec = DeserializeSector(rSec);

                        if (fSec == null)
                            break; //Not yet supported sector type

                        tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard, jSec.gradingStandard == null ? false : jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

            if (tempSectors.Count > 0)
            {
                IsStore = true;
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    RepeatSectors.Add(sec);
            }
            //}
            return true;
        }
        private void ClearReadAction(object parameter)
        {
            RepeatImage = null;
            IsGoldenRepeat = false;
            RepeatSectors.Clear();
            ReadJob = null;
            IsStore = false;

            StandardsDatabase.Row row = StandardsDatabase.GetRow(GradingStandard, LabelImageUID);

            if (row == null)
                return;

            RepeatImage = row.RepeatImage;
            IsGoldenRepeat = true;
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

        public void Clear()
        {
            LabelImage = null;
            RepeatImage = null;
            ReadJob = null;

            LabelTemplate = null;

            foreach (var sec in RepeatSectors)
                sec.Clear();

            RepeatSectors.Clear();

            foreach (var sec in LabelSectors)
                sec.Clear();

            LabelSectors.Clear();

            dialogCoordinator = null;
            StandardsDatabase = null;
            V275 = null;
        }
    }
}
