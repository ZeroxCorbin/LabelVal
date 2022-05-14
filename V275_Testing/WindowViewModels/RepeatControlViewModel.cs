using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    public class RepeatControlViewModel : Core.BaseViewModel
    {
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

        public string JobSegments { get => jobSegments; set => SetProperty(ref jobSegments, value); }
        private string jobSegments;

        public string StoredSegments { get => storedSegments; set => SetProperty(ref storedSegments, value); }
        private string storedSegments;

        public string JobSegmentsResults { get; set; }

        V275_API_Commands V275 { get; }

        V275_Job Job { get; set; }
        V275_Report Report {get; set;}

        public ICommand Print { get; }
        public ICommand Read { get; }
        public ICommand Store { get; }

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


        public RepeatControlViewModel(int repeatNumber, string imagePath, string printerName, string gradingStandard, StandardsDatabase standardsDatabase, V275_API_Commands v275)
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

        private void GetStored()
        {
            StandardsDatabase.Row row = StandardsDatabase.GetRepeat(GradingStandard, RepeatNumber);

            if (row == null)
            {
                StoredSegments = "Nothing stored yet!";
                return;
            }

            if(!string.IsNullOrEmpty(row.Report) && !string.IsNullOrEmpty(row.Job))
                StoredSegments = GetDisplayString(JsonConvert.DeserializeObject<V275_Job>(row.Job), JsonConvert.DeserializeObject<V275_Report>(row.Report));
            else
                StoredSegments = "Nothing stored yet!";
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

            if(!await V275.GetJob())
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

            JobSegments = GetDisplayString(Job, Report);

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

                string s = $"{item.gradingStandard.standard} Table {item.gradingStandard.tableId}";

                if (s != GradingStandard)
                {
                    sb.AppendLine(" WRONG GRADING STANDARD");
                }
                else
                {
                    sb.AppendLine();
                }
                foreach (JObject rep in report.inspectLabel.inspectSector)
                {
                    //V275_Report_Verify1D verify1D;
                    //V275_Report_Verify2D verify2D;
                    if (item.name == rep["name"].ToString())
                        foreach (JObject alm in rep["data"]["alarms"])
                        {
                            sb.Append('\t');
                            sb.Append(alm["name"]);
                            sb.Append(": ");
                            sb.Append(alm["data"]["subAlarm"]);
                            sb.Append(" ");
                            sb.Append(alm["data"]["text"]);
                            sb.Append(" / ");
                            sb.Append(alm["data"]["expected"]);
                            sb.AppendLine();
                        }
                    //if (type == "verify1D")
                    //    verify1D = JsonConvert.DeserializeObject<V275_Report_Verify1D>(rep.ToString());
                    //else if (type == "verify2D")
                    //    verify2D = JsonConvert.DeserializeObject<V275_Report_Verify2D>(rep.ToString());


                }
            }

           return sb.ToString();
        }
    }
}
