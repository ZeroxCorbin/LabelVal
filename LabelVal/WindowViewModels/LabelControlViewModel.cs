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
using LabelVal.Databases;
using LabelVal.Printer;
using LabelVal.Utilities;
using LabelVal.V275;
using LabelVal.V275.Models;
using System.Windows.Media;
using System.Globalization;
using System.Windows;

namespace LabelVal.WindowViewModels
{
    public class LabelControlViewModel : Core.BaseViewModel
    {
        public delegate void PrintingDelegate(LabelControlViewModel label, string type);
        public event PrintingDelegate Printing;

        public delegate void BringIntoViewDelegate();
        public event BringIntoViewDelegate BringIntoView;

        private string labelImageUID;
        public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

        private string labelComment;
        public string LabelComment { get => labelComment; set => SetProperty(ref labelComment, value); }

        private byte[] labelImage;
        public byte[] LabelImage { get => labelImage; set => SetProperty(ref labelImage, value); }

        private byte[] repeatImage = null;
        public byte[] RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

        private DrawingImage repeatOverlay;
        public DrawingImage RepeatOverlay { get => repeatOverlay; set => SetProperty(ref repeatOverlay, value); }

        private StandardsDatabase.Row currentRow;
        public StandardsDatabase.Row CurrentRow { get => currentRow; set => SetProperty(ref currentRow, value); }



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

        private ObservableCollection<SectorDifferenceViewModel> diffSectors = new ObservableCollection<SectorDifferenceViewModel>();
        public ObservableCollection<SectorDifferenceViewModel> DiffSectors { get => diffSectors; set => SetProperty(ref diffSectors, value); }

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

        public bool IsSimulation
        {
            get => isSimulation;
            set { SetProperty(ref isSimulation, value); OnPropertyChanged("IsNotSimulation"); }
        }
        public bool IsNotSimulation => !isSimulation;
        private bool isSimulation = false;

        public bool IsDatabaseLocked
        {
            get => isDatabaseLocked;
            set { SetProperty(ref isDatabaseLocked, value); OnPropertyChanged("IsNotDatabaseLocked"); }
        }
        public bool IsNotDatabaseLocked => !isDatabaseLocked;
        private bool isDatabaseLocked = false;

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
        public V275_Job LabelTemplate { get; set; }
        public V275_Job RepeatTemplate { get; set; }
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

        private BitmapImage CreateBitmap(byte[] data)
        {
            if (data == null || data.Length < 2)
                return null;

            BitmapImage img = new BitmapImage();

            using (MemoryStream memStream = new MemoryStream(data))
            {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = memStream;
                //img.DecodePixelWidth = 400;
                img.EndInit();
                img.Freeze();

            }
            return img;
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
        }

        public void PrintAction(object parameter)
        {
            IsWorking = true;
            IsFaulted = false;

            BringIntoView?.Invoke();
            Printing?.Invoke(this, (string)parameter);
        }

        private void GetStored()
        {
            //foreach (var sec in LabelSectors)
            //    sec.Clear();

            DiffSectors.Clear();
            LabelSectors.Clear();
            IsLoad = false;

            CurrentRow = StandardsDatabase.GetRow(GradingStandard, LabelImageUID);

            if (CurrentRow == null)
            {
                if (RepeatSectors.Count == 0)
                {
                    RepeatImage = null;
                    RepeatOverlay = null;
                    IsGoldenRepeat = false;
                }

                return;
            }

            LabelTemplate = JsonConvert.DeserializeObject<V275_Job>(CurrentRow.LabelTemplate);

            RepeatImage = CurrentRow.RepeatImage;

            IsGoldenRepeat = true;

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            if (!string.IsNullOrEmpty(CurrentRow.LabelReport) && !string.IsNullOrEmpty(CurrentRow.LabelTemplate))
                foreach (var jSec in LabelTemplate.sectors)
                {
                    bool isWrongStandard = false;
                    if (jSec.type == "verify1D" || jSec.type == "verify2D")
                        if (IsGS1Standard)
                        {
                            if (jSec.gradingStandard.enabled)
                                isWrongStandard = !(GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));
                            else
                                isWrongStandard = true;
                        }
                        else
                            isWrongStandard = false;

                    foreach (JObject rSec in JsonConvert.DeserializeObject<V275_Report>(CurrentRow.LabelReport).inspectLabel.inspectSector)
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

            RepeatOverlay = CreateRepeatOverlay(false, false);
        }
        private async void StoreAction(object parameter)
        {
            if (LabelSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this label?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            StandardsDatabase.AddRow(GradingStandard, LabelImageUID, LabelImage, JsonConvert.SerializeObject(RepeatTemplate), JsonConvert.SerializeObject(Report), RepeatImage);
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
        public async Task<bool> Read(int repeat)
        {
            Status = string.Empty;

            //if (!isRunning)
            //{
            RepeatSectors.Clear();
            DiffSectors.Clear();

            IsStore = false;
            //}

            if (!await V275.Read(repeat))
            {
                Status = V275.Status;

                RepeatTemplate = null;
                Report = null;

                if (!IsGoldenRepeat)
                {
                    RepeatImage = null;
                    RepeatOverlay = null;
                }

                return false;
            }

            RepeatTemplate = V275.Commands.Job;
            Report = V275.Commands.Report;
            RepeatImage = ConvertToPng(V275.Commands.Repeatimage);
            IsGoldenRepeat = false;

            //if (!isRunning)
            //{
            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            foreach (var jSec in RepeatTemplate.sectors)
            {
                bool isWrongStandard = false;
                if (jSec.type == "verify1D" || jSec.type == "verify2D")
                    if (IsGS1Standard)
                    {
                        if (jSec.gradingStandard.enabled)
                            isWrongStandard = !(GradingStandard.StartsWith($"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}"));
                        else
                            isWrongStandard = true;
                    }
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
            GetSectorDiff();

            RepeatOverlay = CreateRepeatOverlay(true, true);

            return true;
        }

        private DrawingImage CreateRepeatOverlay(bool isRepeat, bool isDetailed)
        {
            var bmp = CreateBitmap(RepeatImage);

            //Draw the image outline the same size as the repeat image
            GeometryDrawing border = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
                Pen = new Pen(Brushes.Transparent, 1)
            };

            GeometryGroup secAreas = new GeometryGroup();
            DrawingGroup drwGroup = new DrawingGroup();

            if (!isRepeat)
            {
                foreach (var sec in LabelTemplate.sectors)
                {
                    var area = new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height));
                    secAreas.Children.Add(area);
                }

                if (isDetailed)
                    drwGroup = GetModuleGrid(LabelTemplate.sectors, LabelSectors);
            }
            else
            {
                foreach (var sec in RepeatTemplate.sectors)
                {
                    var area = new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height));
                    secAreas.Children.Add(area);
                }

                if (isDetailed)
                    drwGroup = GetModuleGrid(RepeatTemplate.sectors, RepeatSectors);
            }

            GeometryDrawing sectors = new GeometryDrawing
            {
                Geometry = secAreas,
                Pen = new Pen(Brushes.Red, 5)
            };



            //DrawingGroup drwGroup = new DrawingGroup();
            drwGroup.Children.Add(sectors);
            //drwGroup.Children.Add(mGrid);
            drwGroup.Children.Add(border);

            DrawingImage geometryImage = new DrawingImage(drwGroup);
            geometryImage.Freeze();
            return geometryImage;


            //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(bmp.PixelWidth, bmp.PixelHeight);
            //using (var g = System.Drawing.Graphics.FromImage(bitmap))
            //{
            //    using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5))
            //    {
            //        if (!isRepeat)
            //        {
            //            DrawModuleGrid(g, LabelTemplate.sectors, LabelSectors);
            //        }
            //        else
            //        {
            //            DrawModuleGrid(g, RepeatTemplate.sectors, RepeatSectors);
            //        }
            //    }
            //}

            //using (MemoryStream memory = new MemoryStream())
            //{
            //    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            //    memory.Position = 0;
            //    BitmapImage bitmapImage = new BitmapImage();
            //    bitmapImage.BeginInit();
            //    bitmapImage.StreamSource = memory;
            //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmapImage.EndInit();
            //    return bitmapImage;
            //}

            //string text = "Verify1D";
            //Typeface typeface = new Typeface("Arial");
            //if (typeface.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface))
            //{

            //    GlyphRun gr = new GlyphRun
            //    {
            //        PixelsPerDip = 4,
            //        IsSideways = false,
            //        FontRenderingEmSize = 1.0,
            //        BidiLevel = 0,
            //        GlyphTypeface = _glyphTypeface
            //    };

            //    double textWidth = 0;
            //    for (int ix = 0; ix < text.Length; ix++)
            //    {
            //        ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
            //        gr.GlyphIndices.Add(glyphIndex);

            //        double width = _glyphTypeface.AdvanceWidths[glyphIndex] * 8;
            //        gr.AdvanceWidths.Add(width);

            //        textWidth += width;
            //        double textHeight = _glyphTypeface.Height * 8;


            //    }
            //    gr.BaselineOrigin = new System.Windows.Point(0, 0);
            //    GlyphRunDrawing grd = new GlyphRunDrawing(Brushes.Black, gr);
            //    drwGroup.Children.Add(grd);
            //}

        }

        private DrawingGroup GetModuleGrid(V275_Job.Sector[] sectors, ObservableCollection<SectorControlViewModel> parsedSectors)
        {
            DrawingGroup drwGroup = new DrawingGroup();
            //GeometryGroup moduleGrid = new GeometryGroup();

            foreach (var sec in sectors)
            {
                var sect = parsedSectors.FirstOrDefault((e) => e.JobSector.name.Equals(sec.name));

                if (sect != null)
                {
                    GeometryGroup secArea = new GeometryGroup();

                    secArea.Children.Add(new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height)));


                    if (sec.symbology == "qr" || sec.symbology == "dataMatrix")
                    {

                        var res = (V275_Report_InspectSector_Verify2D)sect.ReportSector;

                        if (res.data.extendedData != null)
                        {
                            if (res.data.extendedData.ModuleReflectance != null)
                            {
                                GeometryGroup moduleGrid = new GeometryGroup();
                                DrawingGroup textGrp = new DrawingGroup();

                                var qzX = (sec.symbology == "dataMatrix") ? 1 : res.data.extendedData.QuietZone;
                                var qzY = res.data.extendedData.QuietZone;

                                var dX = (sec.symbology == "dataMatrix") ? 0 : (res.data.extendedData.DeltaX / 2);
                                var dY = (sec.symbology == "dataMatrix") ? (res.data.extendedData.DeltaY * res.data.extendedData.NumRows) : (res.data.extendedData.DeltaY / 2);

                                var startX = 0;// sec.left + res.data.extendedData.Xnw - dX + 1 - (qzX * res.data.extendedData.DeltaX);
                                var startY = 0;// sec.top + res.data.extendedData.Ynw - dY + 1 - (qzY * res.data.extendedData.DeltaY);

                                int cnt = 0;

                                for (int row = -qzX; row < res.data.extendedData.NumRows + qzX; row++)
                                {
                                    for (int col = -qzY; col < res.data.extendedData.NumColumns + qzY; col++)
                                    {
                                        var area1 = new RectangleGeometry(new System.Windows.Rect(startX + (res.data.extendedData.DeltaX * (col + qzX)), startY + (res.data.extendedData.DeltaY * (row + qzY)), res.data.extendedData.DeltaX, res.data.extendedData.DeltaY));
                                        moduleGrid.Children.Add(area1);

                                        string text = res.data.extendedData.ModuleReflectance[cnt].ToString();
                                        Typeface typeface = new Typeface("Arial");
                                        if (typeface.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface))
                                        {
                                            ushort[] _glyphIndexes = new ushort[text.Length];
                                            double[] _advanceWidths = new double[text.Length];

                                            double textWidth = 0;
                                            for (int ix = 0; ix < text.Length; ix++)
                                            {
                                                ushort glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
                                                _glyphIndexes[ix] = glyphIndex;

                                                double width = _glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                                                _advanceWidths[ix] = width;

                                                textWidth += width;
                                            }

                                            GlyphRun gr = new GlyphRun(_glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                                new System.Windows.Point(startX + (res.data.extendedData.DeltaX * (col + qzX)) + 1,
                                                startY + (res.data.extendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.data.extendedData.DeltaY / 4))),
                                                _advanceWidths, null, null, null, null, null, null);


                                            GlyphRunDrawing grd = new GlyphRunDrawing(Brushes.Blue, gr);

                                            textGrp.Children.Add(grd);
                                        }

                                        text = res.data.extendedData.ModuleModulation[cnt++].ToString();
                                        Typeface typeface1 = new Typeface("Arial");
                                        if (typeface1.TryGetGlyphTypeface(out GlyphTypeface _glyphTypeface1))
                                        {
                                            ushort[] _glyphIndexes = new ushort[text.Length];
                                            double[] _advanceWidths = new double[text.Length];

                                            double textWidth = 0;
                                            for (int ix = 0; ix < text.Length; ix++)
                                            {
                                                ushort glyphIndex = _glyphTypeface1.CharacterToGlyphMap[text[ix]];
                                                _glyphIndexes[ix] = glyphIndex;

                                                double width = _glyphTypeface1.AdvanceWidths[glyphIndex] * 2;
                                                _advanceWidths[ix] = width;

                                                textWidth += width;
                                            }

                                            GlyphRun gr = new GlyphRun(_glyphTypeface1, 0, false, 2, 1.0f, _glyphIndexes,
                                                new System.Windows.Point(startX + (res.data.extendedData.DeltaX * (col + qzX)) + 1,
                                                startY + (res.data.extendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.data.extendedData.DeltaY / 2))),
                                                _advanceWidths, null, null, null, null, null, null);

                                            GlyphRunDrawing grd = new GlyphRunDrawing(Brushes.Blue, gr);
                                            textGrp.Children.Add(grd);
                                        }

                                        //FormattedText formattedText = new FormattedText(
                                        //    res.data.extendedData.ModuleReflectance[row + col].ToString(),
                                        //    CultureInfo.GetCultureInfo("en-us"),
                                        //    FlowDirection.LeftToRight,
                                        //    new Typeface("Arial"),
                                        //    4,
                                        //    System.Windows.Media.Brushes.Black // This brush does not matter since we use the geometry of the text.
                                        //);

                                        //// Build the geometry object that represents the text.
                                        //Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(startX + (res.data.extendedData.DeltaX * row), startY + (res.data.extendedData.DeltaY * col)));
                                        //moduleGrid.Children.Add(textGeometry);
                                    }
                                }

                                TransformGroup transGroup = new TransformGroup();


                                transGroup.Children.Add(new RotateTransform(
                                    sec.orientation,
                                    (res.data.extendedData.DeltaX * (res.data.extendedData.NumColumns + (qzX * 2))) / 2,
                                    (res.data.extendedData.DeltaY * (res.data.extendedData.NumRows + (qzY * 2))) / 2));

                                transGroup.Children.Add(new TranslateTransform(sec.left, sec.top));

                                //transGroup.Children.Add(new TranslateTransform (res.data.extendedData.Xnw - dX + 1 - (qzX * res.data.extendedData.DeltaX), res.data.extendedData.Ynw - dY + 1 - (qzY * res.data.extendedData.DeltaY)));
                                if (sec.orientation == 0)
                                    transGroup.Children.Add(new TranslateTransform(
                                        res.data.extendedData.Xnw - (qzX * res.data.extendedData.DeltaX) - dX + 1,
                                        res.data.extendedData.Ynw - (qzY * res.data.extendedData.DeltaY) - dY + 1));

                                //works for dataMatrix
                                //if (sec.orientation == 90)
                                //    transGroup.Children.Add(new TranslateTransform(
                                //         sec.width - res.data.extendedData.Ynw - (qzY * res.data.extendedData.DeltaY) - 1, 
                                //         res.data.extendedData.Xnw - (qzX * res.data.extendedData.DeltaX) - dX + 1));


                                if (sec.orientation == 90)
                                {
                                    double x;
                                    if (sec.symbology == "dataMatrix")
                                        x = sec.width - res.data.extendedData.Ynw - (qzY * res.data.extendedData.DeltaY) - 1;
                                    else
                                        x = sec.width - res.data.extendedData.Ynw - dY - (res.data.extendedData.NumColumns + qzY) * res.data.extendedData.DeltaY;

                                    transGroup.Children.Add(new TranslateTransform(
                                         x,
                                         res.data.extendedData.Xnw - (qzX * res.data.extendedData.DeltaX) - dX + 1));
                                }

                                if (sec.orientation == 180)
                                {
                                    transGroup.Children.Add(new TranslateTransform(
                                        res.data.extendedData.Xnw - (qzX * res.data.extendedData.DeltaX) - dX + 1,
                                        res.data.extendedData.Ynw - (qzY * res.data.extendedData.DeltaY) - dY + 1));
                                }




                                moduleGrid.Transform = transGroup;
                                textGrp.Transform = transGroup;

                                GeometryDrawing mGrid = new GeometryDrawing
                                {
                                    Geometry = moduleGrid,
                                    Pen = new Pen(Brushes.Yellow, 0.25)
                                };


                                drwGroup.Children.Add(mGrid);
                                drwGroup.Children.Add(textGrp);
                            }
                        }
                    }
                }
            }

            //GeometryDrawing mGrid = new GeometryDrawing
            //{
            //    Geometry = moduleGrid,
            //    Pen = new Pen(Brushes.Yellow, 0.25)
            //};


            //drwGroup.Children.Add(mGrid);

            return drwGroup;
        }

        private GeometryGroup DrawModuleGrid(System.Drawing.Graphics g, V275_Job.Sector[] sectors, ObservableCollection<SectorControlViewModel> parsedSectors)
        {
            GeometryGroup moduleGrid = new GeometryGroup();
            using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5))
            {
                using (System.Drawing.Pen p1 = new System.Drawing.Pen(System.Drawing.Brushes.Yellow, 0.025f))
                {
                    using (System.Drawing.Brush b = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
                    {
                        foreach (var sec in sectors)
                        {

                            g.DrawRectangle(p, new System.Drawing.Rectangle(sec.left, sec.top, sec.width, sec.height));
                            //g.DrawString(sec.username, new System.Drawing.Font("Arial", 84, System.Drawing.FontStyle.Bold), System.Drawing.Brushes.Red, new System.Drawing.PointF(sec.left, sec.top - 100));

                            if (sec.type == "verify2D")
                            {

                                var sect = parsedSectors.FirstOrDefault((e) => e.JobSector.name.Equals(sec.name));

                                if (sect != null)
                                {
                                    var res = (V275_Report_InspectSector_Verify2D)sect.ReportSector;

                                    if (res.data.extendedData != null)
                                    {
                                        if (res.data.extendedData.ModuleReflectance != null)
                                        {
                                            //var startX = (area.Rect.Left + (area.Rect.Width / 2)) - ((res.data.extendedData.NumColumns / 2) * res.data.extendedData.DeltaX);
                                            //var startY = (area.Rect.Top + (area.Rect.Height / 2)) - ((res.data.extendedData.NumRows / 2) * res.data.extendedData.DeltaY);
                                            var startX = Math.Round(sec.left + res.data.extendedData.Xnw - (res.data.extendedData.DeltaX / 2) + 1);
                                            var startY = Math.Round(sec.top + res.data.extendedData.Ynw - (res.data.extendedData.DeltaY / 2) + 1);
                                            //var endX = (area.Rect.Left + (area.Rect.Width / 2)) + ((res.data.extendedData.NumColumns / 2) * res.data.extendedData.DeltaX);
                                            //var endY = (area.Rect.Top + (area.Rect.Height / 2)) + ((res.data.extendedData.NumRows / 2) * res.data.extendedData.DeltaY);

                                            for (int row = 0; row < res.data.extendedData.NumRows; row++)
                                            {
                                                for (int col = 0; col < res.data.extendedData.NumColumns; col++)
                                                {
                                                    g.DrawRectangle(p1, new System.Drawing.Rectangle((int)(startX + (res.data.extendedData.DeltaX * row)), (int)(startY + (res.data.extendedData.DeltaY * col)), (int)res.data.extendedData.DeltaX, (int)res.data.extendedData.DeltaX));
                                                    //var area1 = new RectangleGeometry(new System.Windows.Rect(startX + (res.data.extendedData.DeltaX * row), startY + (res.data.extendedData.DeltaY * col), res.data.extendedData.DeltaX, res.data.extendedData.DeltaX));
                                                    //moduleGrid.Children.Add(area1);

                                                    //FormattedText formattedText = new FormattedText(
                                                    //    res.data.extendedData.ModuleReflectance[row+col].ToString(),
                                                    //    CultureInfo.GetCultureInfo("en-us"),
                                                    //    FlowDirection.LeftToRight,
                                                    //    new Typeface("Arial"),
                                                    //    4,
                                                    //    System.Windows.Media.Brushes.Black // This brush does not matter since we use the geometry of the text.
                                                    //);
                                                    g.DrawString(res.data.extendedData.ModuleReflectance[row + col].ToString(), new System.Drawing.Font("Arial", 4), b, new System.Drawing.Point((int)(startX + (res.data.extendedData.DeltaX * row)), (int)(startY + (res.data.extendedData.DeltaY * col))));
                                                    //// Build the geometry object that represents the text.
                                                    //Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(startX + (res.data.extendedData.DeltaX * row), startY + (res.data.extendedData.DeltaY * col)));
                                                    //moduleGrid.Children.Add(textGeometry);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return moduleGrid;
        }


        private byte[] ConvertToPng(byte[] img)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            using (var ms = new System.IO.MemoryStream(img))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Frames.Add(BitmapFrame.Create(ms));
                    encoder.Save(stream);
                    stream.Close();

                    return stream.ToArray();

                }
            }
        }

        private void ClearReadAction(object parameter)
        {
            RepeatTemplate = null;
            RepeatImage = null;
            RepeatOverlay = null;
            RepeatSectors.Clear();

            DiffSectors.Clear();

            IsStore = false;

            CurrentRow = StandardsDatabase.GetRow(GradingStandard, LabelImageUID);

            if (CurrentRow == null)
            {
                IsGoldenRepeat = false;
                RepeatImage = null;
                RepeatOverlay = null;
                return;
            }

            RepeatImage = CurrentRow.RepeatImage;
            RepeatOverlay = CreateRepeatOverlay(false, false);
            IsGoldenRepeat = true;
        }

        private void GetSectorDiff()
        {
            List<SectorDifferenceViewModel> diff = new List<SectorDifferenceViewModel>();

            //Compare; Do not check for missing her. To keep found at top of list.
            foreach (var sec in LabelSectors)
            {
                foreach (var cSec in RepeatSectors)
                    if (sec.JobSector.name == cSec.JobSector.name)
                    {
                        if (sec.JobSector.symbology == cSec.JobSector.symbology)
                        {
                            diff.Add(sec.SectorResults.Compare(cSec.SectorResults));
                            continue;
                        }
                        else
                        {
                            var dat = new SectorDifferenceViewModel
                            {
                                UserName = $"{sec.JobSector.username} (SYMBOLOGY MISMATCH)",
                                IsSectorMissing = true,
                                SectorMissingText = $"Label Sector {sec.JobSector.symbology} : Repeat Sector {cSec.JobSector.symbology}"
                            };
                            diff.Add(dat);
                        }
                    }
            }

            //Check for missing
            foreach (var sec in LabelSectors)
            {
                bool found = false;
                foreach (var cSec in RepeatSectors)
                    if (sec.JobSector.name == cSec.JobSector.name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    var dat = new SectorDifferenceViewModel
                    {
                        UserName = $"{sec.JobSector.username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Repeat Sectors"
                    };
                    diff.Add(dat);
                }

            }

            //check for missing
            if (LabelSectors.Count > 0)
                foreach (var sec in RepeatSectors)
                {
                    bool found = false;
                    foreach (var cSec in LabelSectors)
                        if (sec.JobSector.name == cSec.JobSector.name)
                        {
                            found = true;
                            continue;
                        }

                    if (!found)
                    {
                        var dat = new SectorDifferenceViewModel
                        {
                            UserName = $"{sec.JobSector.username} (MISSING)",
                            IsSectorMissing = true,
                            SectorMissingText = "Not found in Label Sectors"
                        };
                        diff.Add(dat);
                    }

                }

            foreach (var d in diff)
                DiffSectors.Add(d);

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

        //public void Clear()
        //{
        //    LabelImage = null;
        //    RepeatImage = null;
        //    RepeatTemplate = null;

        //    LabelTemplate = null;

        //    foreach (var sec in RepeatSectors)
        //        sec.Clear();

        //    RepeatSectors.Clear();

        //    foreach (var sec in LabelSectors)
        //        sec.Clear();

        //    LabelSectors.Clear();

        //    dialogCoordinator = null;
        //    StandardsDatabase = null;
        //    V275 = null;
        //}
    }
}
