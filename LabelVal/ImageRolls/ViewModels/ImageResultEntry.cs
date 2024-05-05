using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V275_REST_lib;
using V275_REST_lib.Models;

namespace LabelVal.ImageRolls.ViewModels;

public partial class ImageResultEntry : ObservableRecipient, IRecipient<NodeMessages.SelectedNodeChanged>, IRecipient<DatabaseMessages.SelectedDatabseChanged>, IRecipient<ScannerMessages.SelectedScannerChanged>
{
    public delegate void PrintingDelegate(ImageResultEntry label, string type);
    public event PrintingDelegate Printing;

    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;

    public delegate void StatusChange(string status);
    public event StatusChange StatusChanged; 

    [ObservableProperty] private Databases.ImageResults.V275Result currentRow;

    [ObservableProperty] private byte[] sourceImage;
    [ObservableProperty] private string sourceImageUID;
    [ObservableProperty] private string sourceImageComment;

    public Job LabelTemplate { get; set; }
    public Job RepeatTemplate { get; set; }
    public V275_REST_lib.Models.Report RepeatReport { get; private set; }

    [ObservableProperty] private ObservableCollection<Sectors> v275StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors> v275CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<SectorDifferences> v275DiffSectors = [];

    [ObservableProperty] private byte[] v275Image = null;
    [ObservableProperty] private DrawingImage v275ImageStoredSectorsOverlay;
    [ObservableProperty] private bool isV275ImageStored;

    [ObservableProperty] private ObservableCollection<Sectors> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<SectorDifferences> v5DiffSectors = [];

    [ObservableProperty] private int printCount = 1;


    [ObservableProperty] private bool isStore = false;
    partial void OnIsStoreChanged(bool value) => OnPropertyChanged(nameof(IsNotStore));
    public bool IsNotStore => !IsStore;


    [ObservableProperty] private bool isLoad = false;
    partial void OnIsLoadChanged(bool value) => OnPropertyChanged(nameof(IsNotLoad));
    public bool IsNotLoad => !IsLoad;

    [ObservableProperty] private bool isWorking = false;
    partial void OnIsWorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotWorking));
    public bool IsNotWorking => !IsWorking;


    [ObservableProperty] private bool isFaulted = false;
    partial void OnIsFaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotFaulted));
    public bool IsNotFaulted => !IsFaulted;

    public string SourceImagePath { get; }


    [ObservableProperty] private string status;
    partial void OnStatusChanged(string value) => App.Current.Dispatcher.Invoke(() => StatusChanged?.Invoke(Status));

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    [ObservableProperty] private Scanner selectedScanner; 
    [ObservableProperty] private Databases.ImageResults selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResults value) => GetStored();


    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public ImageResultEntry(string imagePath, string imageComment, Node selectedNode, ImageRollEntry selectedImageRoll, Databases.ImageResults selectedDatabase, Scanner selectedScanner)
    {
        SourceImagePath = imagePath;
        SourceImageComment = imageComment;

        GetImage(imagePath);

        SelectedImageRoll = selectedImageRoll;
        SelectedNode = selectedNode;
        SelectedDatabase = selectedDatabase;
        SelectedScanner = selectedScanner;

        IsActive = true;
    }

    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;
    public void Receive(DatabaseMessages.SelectedDatabseChanged message) => SelectedDatabase = message.Value;
    public void Receive(ScannerMessages.SelectedScannerChanged message) => SelectedScanner = message.Value;

    public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
    {

        var result = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

        return result;
    }

    private void GetImage(string imagePath)
    {
        SourceImage = File.ReadAllBytes(imagePath);
        SourceImageUID = ImageUtilities.ImageUID(SourceImage);
    }

    private void GetStored()
    {
        //foreach (var sec in V275StoredSectors)
        //    sec.Clear();

        V275DiffSectors.Clear();
        V275StoredSectors.Clear();
        IsLoad = false;

        CurrentRow = SelectedDatabase.Select_V275Result(SelectedImageRoll.Name, SourceImageUID);

        if (CurrentRow == null)
        {
            if (V275CurrentSectors.Count == 0)
            {
                V275Image = null;
                V275ImageStoredSectorsOverlay = null;
                IsV275ImageStored = false;
            }

            return;
        }

        LabelTemplate = JsonConvert.DeserializeObject<Job>(CurrentRow.SourceImageTemplate);

        V275Image = CurrentRow.StoredImage;
        IsV275ImageStored = true;

        List<Sectors> tempSectors = [];
        if (!string.IsNullOrEmpty(CurrentRow.SourceImageReport) && !string.IsNullOrEmpty(CurrentRow.SourceImageTemplate))
            foreach (var jSec in LabelTemplate.sectors)
            {
                var isWrongStandard = false;
                if (jSec.type is "verify1D" or "verify2D")
                    isWrongStandard = SelectedImageRoll.IsGS1 && (!jSec.gradingStandard.enabled || SelectedImageRoll.TableID != jSec.gradingStandard.tableId);

                foreach (JObject rSec in JsonConvert.DeserializeObject<V275_REST_lib.Models.Report>(CurrentRow.SourceImageReport).inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = DeserializeSector(rSec, false);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

            foreach (var sec in tempSectors)
                V275StoredSectors.Add(sec);

            IsLoad = true;
        }

        V275ImageStoredSectorsOverlay = CreateV275ImageStoredSectorsOverlay(false, false);
    }

    [RelayCommand]
    private void Print(object parameter)
    {
        IsWorking = true;
        IsFaulted = false;

        BringIntoView?.Invoke();
        Printing?.Invoke(this, (string)parameter);
    }
    [RelayCommand]
    private void Save(object parameter)
    {
        var par = (string)parameter;

        SendTo95xxApplication();

        var path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (par == "repeat")
            {
                var bmp = ImageUtilities.ConvertToBmp(V275Image);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
            else
            {
                var bmp = ImageUtilities.ConvertToBmp(SourceImage);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch (Exception)
        {

        }
    }
    [RelayCommand]
    private async Task Store()
    {
        if (V275StoredSectors.Count > 0)
            if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this label?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;

        _ = SelectedDatabase.InsertOrReplace_V275Result(new Databases.ImageResults.V275Result
        {
            ImageRollName = SelectedImageRoll.Name,
            SourceImageUID = SourceImageUID,
            SourceImage = SourceImage,
            SourceImageTemplate = JsonConvert.SerializeObject(LabelTemplate),
            SourceImageReport = JsonConvert.SerializeObject(RepeatReport),
            StoredImage = V275Image
        });

        V275CurrentSectors.Clear();
        IsStore = false;

        GetStored();
    }
    [RelayCommand] private void Read() => _ = ReadTask(0);
    [RelayCommand] private void Load() => _ = LoadTask();
    [RelayCommand] private void Inspect() => _ = ReadTask(0);
    [RelayCommand]
    private async Task ClearStored()
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this label?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            SelectedDatabase.Delete_V275Result(SelectedImageRoll.Name, SourceImageUID);
            GetStored();
        }
    }
    [RelayCommand]
    private void ClearRead()
    {
        RepeatReport = null;
        RepeatTemplate = null;

        V275Image = null;
        V275ImageStoredSectorsOverlay = null;

        IsV275ImageStored = false;

        V275CurrentSectors.Clear();
        V275DiffSectors.Clear();

        IsStore = false;

        CurrentRow = SelectedDatabase.Select_V275Result(SelectedImageRoll.Name, SourceImageUID);

        if (CurrentRow == null)
            return;

        V275Image = CurrentRow.StoredImage;
        V275ImageStoredSectorsOverlay = CreateV275ImageStoredSectorsOverlay(false, false);
        IsV275ImageStored = true;
    }

    [RelayCommand] private void RedoFiducial() => ImageUtilities.RedrawFiducial(SourceImagePath, false);

    public async Task<bool> ReadTask(int repeat)
    {
        Status = string.Empty;

        V275CurrentSectors.Clear();
        IsStore = false;

        V275DiffSectors.Clear();

        Controller.FullReport report;
        if ((report = await SelectedNode.Connection.Read(repeat, !SelectedNode.IsSimulator)) == null)
        {
            Status = SelectedNode.Connection.Status;

            RepeatTemplate = null;
            RepeatReport = null;

            if (!IsV275ImageStored)
            {
                V275Image = null;
                V275ImageStoredSectorsOverlay = null;
            }

            return false;
        }

        RepeatTemplate = report.job;
        RepeatReport = report.report;

        if (!SelectedNode.IsSimulator)
        {
            V275Image = ImageUtilities.ConvertToPng(report.image, 600);
            IsV275ImageStored = false;
        }
        else
        {
            if (V275Image == null)
            {
                V275Image = SourceImage.ToArray();
                IsV275ImageStored = false;
            }
        }

        //if (!isRunning)
        //{
        List<Sectors> tempSectors = [];
        foreach (var jSec in RepeatTemplate.sectors)
        {
            var isWrongStandard = false;
            if (jSec.type is "verify1D" or "verify2D")
                isWrongStandard = SelectedImageRoll.IsGS1 && (!jSec.gradingStandard.enabled || SelectedImageRoll.TableID != jSec.gradingStandard.tableId);

            foreach (JObject rSec in RepeatReport.inspectLabel.inspectSector)
            {
                if (jSec.name == rSec["name"].ToString())
                {

                    var fSec = DeserializeSector(rSec, !SelectedImageRoll.IsGS1 && SelectedNode.IsOldISO);

                    if (fSec == null)
                        break; //Not yet supported sector type

                    tempSectors.Add(new Sectors(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                    break;
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            IsStore = true;
            tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

            foreach (var sec in tempSectors)
                V275CurrentSectors.Add(sec);
        }
        //}
        GetSectorDiff();

        V275ImageStoredSectorsOverlay = CreateV275ImageStoredSectorsOverlay(true, true);

        return true;
    }
    public async Task<int> LoadTask()
    {
        if (!await SelectedNode.Connection.DeleteSectors())
        {
            Status = SelectedNode.Connection.Status;
            return -1;
        }

        if (V275StoredSectors.Count == 0)
        {
            if (!await SelectedNode.Connection.DetectSectors())
            {
                Status = SelectedNode.Connection.Status;
                return -1;
            }

            return 2;
        }

        foreach (var sec in V275StoredSectors)
        {
            if (!await SelectedNode.Connection.AddSector(sec.JobSector.name, JsonConvert.SerializeObject(sec.JobSector)))
            {
                Status = SelectedNode.Connection.Status;
                return -1;
            }

            if (sec.JobSector.type == "blemish")
            {
                foreach (var layer in sec.JobSector.blemishMask.layers)
                {
                    if (!await SelectedNode.Connection.AddMask(sec.JobSector.name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                        {
                            Status = SelectedNode.Connection.Status;
                            return -1;
                        }
                    }
                }
            }
        }

        return 1;
    }

    //const UInt32 WM_KEYDOWN = 0x0100;
    //const int VK_F5 = 0x74;

    //[DllImport("user32.dll")]
    //static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

    private void SendTo95xxApplication() => _ = Process.GetProcessesByName("LVS-95XX");//foreach (Process proc in processes)//    PostMessage(proc.MainWindowHandle, WM_KEYDOWN, VK_F5, 0);
    private string GetSaveFilePath()
    {
        var saveFileDialog1 = new SaveFileDialog();
        saveFileDialog1.Filter = "Bitmap Image|*.bmp";//|Gif Image|*.gif|JPeg Image|*.jpg";
        saveFileDialog1.Title = "Save an Image File";
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }
    private string SaveImageBytesToFile(string path, byte[] img)
    {
        File.WriteAllBytes(path, img);

        return "";
    }

    private DrawingImage CreateV275ImageStoredSectorsOverlay(bool isRepeat, bool isDetailed)
    {
        var bmp = ImageUtilities.CreateBitmap(V275Image);

        //Draw the image outline the same size as the repeat image
        var border = new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new System.Windows.Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
            Pen = new Pen(Brushes.Transparent, 1)
        };

        var secAreas = new GeometryGroup();
        var drwGroup = new DrawingGroup();

        if (!isRepeat)
        {
            foreach (var sec in LabelTemplate.sectors)
            {
                var area = new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height));
                secAreas.Children.Add(area);
            }

            if (isDetailed)
                drwGroup = GetModuleGrid(LabelTemplate.sectors, V275StoredSectors);
        }
        else
        {
            foreach (var sec in RepeatTemplate.sectors)
            {
                var area = new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height));
                secAreas.Children.Add(area);
            }

            if (isDetailed)
                drwGroup = GetModuleGrid(RepeatTemplate.sectors, V275CurrentSectors);
        }

        var sectors = new GeometryDrawing
        {
            Geometry = secAreas,
            Pen = new Pen(Brushes.Red, 5)
        };

        //DrawingGroup drwGroup = new DrawingGroup();
        drwGroup.Children.Add(sectors);
        //drwGroup.Children.Add(mGrid);
        drwGroup.Children.Add(border);

        var geometryImage = new DrawingImage(drwGroup);
        geometryImage.Freeze();
        return geometryImage;

        //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(bmp.PixelWidth, bmp.PixelHeight);
        //using (var g = System.Drawing.Graphics.FromImage(bitmap))
        //{
        //    using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5))
        //    {
        //        if (!isRepeat)
        //        {
        //            DrawModuleGrid(g, LabelTemplate.sectors, V275StoredSectors);
        //        }
        //        else
        //        {
        //            DrawModuleGrid(g, RepeatTemplate.sectors, V275CurrentSectors);
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

    private DrawingGroup GetModuleGrid(Job.Sector[] sectors, ObservableCollection<Sectors> parsedSectors)
    {
        var drwGroup = new DrawingGroup();
        //GeometryGroup moduleGrid = new GeometryGroup();

        foreach (var sec in sectors)
        {
            var sect = parsedSectors.FirstOrDefault((e) => e.JobSector.name.Equals(sec.name));

            if (sect != null)
            {
                var secArea = new GeometryGroup();

                secArea.Children.Add(new RectangleGeometry(new System.Windows.Rect(sec.left, sec.top, sec.width, sec.height)));

                if (sec.symbology is "qr" or "dataMatrix")
                {

                    var res = (Report_InspectSector_Verify2D)sect.ReportSector;

                    if (res.data.extendedData != null)
                    {
                        if (res.data.extendedData.ModuleReflectance != null)
                        {
                            var moduleGrid = new GeometryGroup();
                            var textGrp = new DrawingGroup();

                            var qzX = (sec.symbology == "dataMatrix") ? 1 : res.data.extendedData.QuietZone;
                            var qzY = res.data.extendedData.QuietZone;

                            var dX = (sec.symbology == "dataMatrix") ? 0 : (res.data.extendedData.DeltaX / 2);
                            var dY = (sec.symbology == "dataMatrix") ? (res.data.extendedData.DeltaY * res.data.extendedData.NumRows) : (res.data.extendedData.DeltaY / 2);

                            var startX = 0;// sec.left + res.data.extendedData.Xnw - dX + 1 - (qzX * res.data.extendedData.DeltaX);
                            var startY = 0;// sec.top + res.data.extendedData.Ynw - dY + 1 - (qzY * res.data.extendedData.DeltaY);

                            var cnt = 0;

                            for (var row = -qzX; row < res.data.extendedData.NumRows + qzX; row++)
                            {
                                for (var col = -qzY; col < res.data.extendedData.NumColumns + qzY; col++)
                                {
                                    var area1 = new RectangleGeometry(new System.Windows.Rect(startX + (res.data.extendedData.DeltaX * (col + qzX)), startY + (res.data.extendedData.DeltaY * (row + qzY)), res.data.extendedData.DeltaX, res.data.extendedData.DeltaY));
                                    moduleGrid.Children.Add(area1);

                                    var text = res.data.extendedData.ModuleModulation[cnt].ToString();
                                    var typeface = new Typeface("Arial");
                                    if (typeface.TryGetGlyphTypeface(out var _glyphTypeface))
                                    {
                                        var _glyphIndexes = new ushort[text.Length];
                                        var _advanceWidths = new double[text.Length];

                                        double textWidth = 0;
                                        for (var ix = 0; ix < text.Length; ix++)
                                        {
                                            var glyphIndex = _glyphTypeface.CharacterToGlyphMap[text[ix]];
                                            _glyphIndexes[ix] = glyphIndex;

                                            var width = _glyphTypeface.AdvanceWidths[glyphIndex] * 2;
                                            _advanceWidths[ix] = width;

                                            textWidth += width;
                                        }

                                        var gr = new GlyphRun(_glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                            new System.Windows.Point(startX + (res.data.extendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.data.extendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.data.extendedData.DeltaY / 4))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        var grd = new GlyphRunDrawing(Brushes.Blue, gr);

                                        textGrp.Children.Add(grd);
                                    }

                                    text = res.data.extendedData.ModuleReflectance[cnt++].ToString();
                                    var typeface1 = new Typeface("Arial");
                                    if (typeface1.TryGetGlyphTypeface(out var _glyphTypeface1))
                                    {
                                        var _glyphIndexes = new ushort[text.Length];
                                        var _advanceWidths = new double[text.Length];

                                        double textWidth = 0;
                                        for (var ix = 0; ix < text.Length; ix++)
                                        {
                                            var glyphIndex = _glyphTypeface1.CharacterToGlyphMap[text[ix]];
                                            _glyphIndexes[ix] = glyphIndex;

                                            var width = _glyphTypeface1.AdvanceWidths[glyphIndex] * 2;
                                            _advanceWidths[ix] = width;

                                            textWidth += width;
                                        }

                                        var gr = new GlyphRun(_glyphTypeface1, 0, false, 2, 1.0f, _glyphIndexes,
                                            new System.Windows.Point(startX + (res.data.extendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.data.extendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.data.extendedData.DeltaY / 2))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        var grd = new GlyphRunDrawing(Brushes.Blue, gr);
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

                            var transGroup = new TransformGroup();

                            transGroup.Children.Add(new RotateTransform(
                                sec.orientation,
                                res.data.extendedData.DeltaX * (res.data.extendedData.NumColumns + (qzX * 2)) / 2,
                                res.data.extendedData.DeltaY * (res.data.extendedData.NumRows + (qzY * 2)) / 2));

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
                                var x = sec.symbology == "dataMatrix"
                                    ? sec.width - res.data.extendedData.Ynw - (qzY * res.data.extendedData.DeltaY) - 1
                                    : sec.width - res.data.extendedData.Ynw - dY - ((res.data.extendedData.NumColumns + qzY) * res.data.extendedData.DeltaY);
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

                            var mGrid = new GeometryDrawing
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

    private GeometryGroup DrawModuleGrid(System.Drawing.Graphics g, Job.Sector[] sectors, ObservableCollection<Sectors> parsedSectors)
    {
        var moduleGrid = new GeometryGroup();
        using (var p = new System.Drawing.Pen(System.Drawing.Brushes.Red, 5))
        {
            using var p1 = new System.Drawing.Pen(System.Drawing.Brushes.Yellow, 0.025f);
            using System.Drawing.Brush b = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow);
            foreach (var sec in sectors)
            {

                g.DrawRectangle(p, new System.Drawing.Rectangle(sec.left, sec.top, sec.width, sec.height));
                //g.DrawString(sec.username, new System.Drawing.Font("Arial", 84, System.Drawing.FontStyle.Bold), System.Drawing.Brushes.Red, new System.Drawing.PointF(sec.left, sec.top - 100));

                if (sec.type == "verify2D")
                {

                    var sect = parsedSectors.FirstOrDefault((e) => e.JobSector.name.Equals(sec.name));

                    if (sect != null)
                    {
                        var res = (Report_InspectSector_Verify2D)sect.ReportSector;

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

                                for (var row = 0; row < res.data.extendedData.NumRows; row++)
                                {
                                    for (var col = 0; col < res.data.extendedData.NumColumns; col++)
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

        return moduleGrid;
    }

    private void GetSectorDiff()
    {
        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in V275StoredSectors)
        {
            foreach (var cSec in V275CurrentSectors)
                if (sec.JobSector.name == cSec.JobSector.name)
                {
                    if (sec.JobSector.symbology == cSec.JobSector.symbology)
                    {
                        diff.Add(sec.SectorResults.Compare(cSec.SectorResults));
                        continue;
                    }
                    else
                    {
                        var dat = new SectorDifferences
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
        foreach (var sec in V275StoredSectors)
        {
            var found = false;
            foreach (var cSec in V275CurrentSectors)
                if (sec.JobSector.name == cSec.JobSector.name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                var dat = new SectorDifferences
                {
                    UserName = $"{sec.JobSector.username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in Repeat Sectors"
                };
                diff.Add(dat);
            }
        }

        //check for missing
        if (V275StoredSectors.Count > 0)
            foreach (var sec in V275CurrentSectors)
            {
                var found = false;
                foreach (var cSec in V275StoredSectors)
                    if (sec.JobSector.name == cSec.JobSector.name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    var dat = new SectorDifferences
                    {
                        UserName = $"{sec.JobSector.username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Label Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (var d in diff)
            V275DiffSectors.Add(d);

    }

    private object DeserializeSector(JObject reportSec, bool removeGS1Data)
    {
        if (reportSec["type"].ToString() == "verify1D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify1D>(reportSec.ToString());
        }
        else if (reportSec["type"].ToString() == "verify2D")
        {
            if (removeGS1Data)
                _ = ((JObject)reportSec["data"]).Remove("gs1SymbolQuality");

            return JsonConvert.DeserializeObject<Report_InspectSector_Verify2D>(reportSec.ToString());
        }
        else
        {
            return reportSec["type"].ToString() == "ocr"
                ? JsonConvert.DeserializeObject<Report_InspectSector_OCR>(reportSec.ToString())
                : reportSec["type"].ToString() == "ocv"
                            ? JsonConvert.DeserializeObject<Report_InspectSector_OCV>(reportSec.ToString())
                            : reportSec["type"].ToString() == "blemish"
                                        ? JsonConvert.DeserializeObject<Report_InspectSector_Blemish>(reportSec.ToString())
                                        : (object)null;
        }
    }



    //public void Clear()
    //{
    //    SourceImage = null;
    //    V275Image = null;
    //    RepeatTemplate = null;

    //    LabelTemplate = null;

    //    foreach (var sec in V275CurrentSectors)
    //        sec.Clear();

    //    V275CurrentSectors.Clear();

    //    foreach (var sec in V275StoredSectors)
    //        sec.Clear();

    //    V275StoredSectors.Clear();

    //    dialogCoordinator = null;
    //    ImageResultsDatabase = null;
    //    V275 = null;
    //}
}
