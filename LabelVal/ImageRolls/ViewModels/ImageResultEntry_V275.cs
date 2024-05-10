using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V275_REST_lib.Models;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResultEntry
{

    [ObservableProperty] private Databases.ImageResults.V275Result v275ResultRow;

    public delegate void V275ProcessImageDelegate(ImageResultEntry imageResults, string type);
    public event V275ProcessImageDelegate V275ProcessImage;

    public Job V275CurrentTemplate { get; set; }
    public Report V275CurrentReport { get; private set; }
    public Job V275StoredTemplate { get; set; }


    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v275CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v275StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> v275DiffSectors = [];


    //partial void OnV275SelectedSectorChanged(Sectors.ViewModels.Sector value) => _ = WeakReferenceMessenger.Default.Send(new SectorMessages.SelectedSectorChanged(value));

    [ObservableProperty] private byte[] v275Image = null;
    [ObservableProperty] private DrawingImage v275SectorsImageOverlay;
    [ObservableProperty] private bool isV275ImageStored;

    [ObservableProperty] private bool isV275Working = false;
    partial void OnIsV275WorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Working));
    public bool IsNotV275Working => !IsV275Working;


    [ObservableProperty] private bool isV275Faulted = false;
    partial void OnIsV275FaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotV275Faulted));
    public bool IsNotV275Faulted => !IsV275Faulted;

    [RelayCommand]
    private void V275Process(string imageType)
    {

        IsV275Working = true;
        IsV275Faulted = false;

        BringIntoView?.Invoke();
        V275ProcessImage?.Invoke(this, imageType);
    }
    [RelayCommand] private Task<bool> V275Read() => V275ReadTask(0);
    [RelayCommand] private Task<int> V275Load() => V275LoadTask();
    //[RelayCommand] private void V275Inspect() => _ = V275ReadTask(0);

    private void V275GetStored()
    {

        //foreach (var sec in V275StoredSectors)
        //    sec.Clear();

        V275DiffSectors.Clear();
        V275StoredSectors.Clear();

        V275ResultRow = SelectedDatabase.Select_V275Result(SelectedImageRoll.Name, SourceImageUID);

        if (V275ResultRow == null)
        {
            if (V275CurrentSectors.Count == 0)
            {
                V275Image = null;
                V275SectorsImageOverlay = null;
                IsV275ImageStored = false;
            }

            return;
        }

        V275StoredTemplate = JsonConvert.DeserializeObject<Job>(V275ResultRow.Template);

        V275Image = V275ResultRow.StoredImage;
        IsV275ImageStored = true;

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(V275ResultRow.Report) && !string.IsNullOrEmpty(V275ResultRow.Template))
            foreach (var jSec in V275StoredTemplate.sectors)
            {
                var isWrongStandard = false;
                if (jSec.type is "verify1D" or "verify2D")
                    isWrongStandard = SelectedImageRoll.IsGS1 && (!jSec.gradingStandard.enabled || SelectedImageRoll.TableID != jSec.gradingStandard.tableId);

                foreach (JObject rSec in JsonConvert.DeserializeObject<Report>(V275ResultRow.Report).inspectLabel.inspectSector)
                {
                    if (jSec.name == rSec["name"].ToString())
                    {

                        var fSec = V275DeserializeSector(rSec, false);

                        if (fSec == null)
                            break;

                        tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                        break;
                    }
                }
            }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                V275StoredSectors.Add(sec);
        }

        V275SectorsImageOverlay = V275CreateSectorsImageOverlay(true, false);


    }
    public async Task<bool> V275ReadTask(int repeat)
    {
        V275CurrentSectors.Clear();
        V275DiffSectors.Clear();

        V275_REST_lib.Controller.FullReport report;
        if ((report = await SelectedNode.Connection.Read(repeat, !SelectedNode.IsSimulator)) == null)
        {
            SendStatusMessage(SelectedNode.Connection.Status, SystemMessages.StatusMessageType.Error);

            V275CurrentTemplate = null;
            V275CurrentReport = null;

            if (!IsV275ImageStored)
            {
                V275Image = null;
                V275SectorsImageOverlay = null;
            }

            return false;
        }

        V275CurrentTemplate = report.job;
        V275CurrentReport = report.report;

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
        List<Sectors.ViewModels.Sector> tempSectors = [];
        foreach (var jSec in V275CurrentTemplate.sectors)
        {
            var isWrongStandard = false;
            if (jSec.type is "verify1D" or "verify2D")
                isWrongStandard = SelectedImageRoll.IsGS1 && (!jSec.gradingStandard.enabled || SelectedImageRoll.TableID != jSec.gradingStandard.tableId);

            foreach (JObject rSec in V275CurrentReport.inspectLabel.inspectSector)
            {
                if (jSec.name == rSec["name"].ToString())
                {

                    var fSec = V275DeserializeSector(rSec, !SelectedImageRoll.IsGS1 && SelectedNode.IsOldISO);

                    if (fSec == null)
                        break; //Not yet supported sector type

                    tempSectors.Add(new Sectors.ViewModels.Sector(jSec, fSec, isWrongStandard, jSec.gradingStandard != null && jSec.gradingStandard.enabled));

                    break;
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                V275CurrentSectors.Add(sec);
        }
        //}
        V275GetSectorDiff();

        V275SectorsImageOverlay = V275CreateSectorsImageOverlay(false, true);

        return true;
    }
    private void V275GetSectorDiff()
    {
        List<Sectors.ViewModels.SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in V275StoredSectors)
        {
            foreach (var cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.Symbology == cSec.Template.Symbology)
                    {
                        diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                        continue;
                    }
                    else
                    {
                        var dat = new Sectors.ViewModels.SectorDifferences
                        {
                            UserName = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Template.Symbology} : Current Sector {cSec.Template.Symbology}"
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
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                var dat = new Sectors.ViewModels.SectorDifferences
                {
                    UserName = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
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
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    var dat = new Sectors.ViewModels.SectorDifferences
                    {
                        UserName = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (var d in diff)
            V275DiffSectors.Add(d);

    }
    public async Task<int> V275LoadTask()
    {
        if (!await SelectedNode.Connection.DeleteSectors())
        {
            SendStatusMessage(SelectedNode.Connection.Status, SystemMessages.StatusMessageType.Error);
            return -1;
        }

        if (V275StoredSectors.Count == 0)
        {
            if (!await SelectedNode.Connection.DetectSectors())
            {
                SendStatusMessage(SelectedNode.Connection.Status, SystemMessages.StatusMessageType.Error);
                return -1;
            }

            return 2;
        }

        foreach (var sec in V275StoredSectors)
        {
            if (!await SelectedNode.Connection.AddSector(sec.Template.Name, JsonConvert.SerializeObject(sec.V275Template)))
            {
                SendStatusMessage(SelectedNode.Connection.Status, SystemMessages.StatusMessageType.Error);
                return -1;
            }

            if (sec.Template.BlemishMask.Layers != null)
            {
                foreach (var layer in sec.Template.BlemishMask.Layers)
                {
                    if (!await SelectedNode.Connection.AddMask(sec.Template.Name, JsonConvert.SerializeObject(layer)))
                    {
                        if (layer.value != 0)
                        {
                            SendStatusMessage(SelectedNode.Connection.Status, SystemMessages.StatusMessageType.Error);
                            return -1;
                        }
                    }
                }
            }
        }

        return 1;
    }
    private DrawingImage V275CreateSectorsImageOverlay(bool useStored, bool isDetailed)
    {
        var bmp = ImageUtilities.CreateBitmap(V275Image);

        //Draw the image outline the same size as the stored image
        var border = new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
            Pen = new Pen(Brushes.Transparent, 1)
        };

        var secAreas = new GeometryGroup();
        var drwGroup = new DrawingGroup();

        if (useStored)
        {
            foreach (var sec in V275StoredTemplate.sectors)
            {
                var area = new RectangleGeometry(new Rect(sec.left, sec.top, sec.width, sec.height));
                secAreas.Children.Add(area);
            }

            if (isDetailed)
                drwGroup = V275GetModuleGrid(V275StoredTemplate.sectors, V275StoredSectors);
        }
        else
        {
            foreach (var sec in V275CurrentTemplate.sectors)
            {
                var area = new RectangleGeometry(new Rect(sec.left, sec.top, sec.width, sec.height));
                secAreas.Children.Add(area);
            }

            if (isDetailed)
                drwGroup = V275GetModuleGrid(V275CurrentTemplate.sectors, V275CurrentSectors);
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
        //            DrawModuleGrid(g, V275StoredTemplate.sectors, V275StoredSectors);
        //        }
        //        else
        //        {
        //            DrawModuleGrid(g, V275CurrentTemplate.sectors, V275CurrentSectors);
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
    private DrawingGroup V275GetModuleGrid(Job.Sector[] sectors, ObservableCollection<Sectors.ViewModels.Sector> parsedSectors)
    {
        var drwGroup = new DrawingGroup();
        //GeometryGroup moduleGrid = new GeometryGroup();

        foreach (var sec in sectors)
        {
            var sect = parsedSectors.FirstOrDefault((e) => e.Template.Name.Equals(sec.name));

            if (sect != null)
            {
                var secArea = new GeometryGroup();

                secArea.Children.Add(new RectangleGeometry(new Rect(sec.left, sec.top, sec.width, sec.height)));

                if (sec.symbology is "qr" or "dataMatrix")
                {

                    var res = (Sectors.ViewModels.Report)sect.Report;

                    if (res.ExtendedData != null)
                    {
                        if (res.ExtendedData.ModuleReflectance != null)
                        {
                            var moduleGrid = new GeometryGroup();
                            var textGrp = new DrawingGroup();

                            var qzX = (sec.symbology == "dataMatrix") ? 1 : res.ExtendedData.QuietZone;
                            var qzY = res.ExtendedData.QuietZone;

                            var dX = (sec.symbology == "dataMatrix") ? 0 : (res.ExtendedData.DeltaX / 2);
                            var dY = (sec.symbology == "dataMatrix") ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                            var startX = 0;// sec.left + res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX);
                            var startY = 0;// sec.top + res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY);

                            var cnt = 0;

                            for (var row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                            {
                                for (var col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                                {
                                    var area1 = new RectangleGeometry(new Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                                    moduleGrid.Children.Add(area1);

                                    var text = res.ExtendedData.ModuleModulation[cnt].ToString();
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
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        var grd = new GlyphRunDrawing(Brushes.Blue, gr);

                                        textGrp.Children.Add(grd);
                                    }

                                    text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
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
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.ExtendedData.DeltaY / 2))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        var grd = new GlyphRunDrawing(Brushes.Blue, gr);
                                        textGrp.Children.Add(grd);
                                    }

                                    //FormattedText formattedText = new FormattedText(
                                    //    res.ExtendedData.ModuleReflectance[row + col].ToString(),
                                    //    CultureInfo.GetCultureInfo("en-us"),
                                    //    FlowDirection.LeftToRight,
                                    //    new Typeface("Arial"),
                                    //    4,
                                    //    System.Windows.Media.Brushes.Black // This brush does not matter since we use the geometry of the text.
                                    //);

                                    //// Build the geometry object that represents the text.
                                    //Geometry textGeometry = formattedText.BuildGeometry(new System.Windows.Point(startX + (res.ExtendedData.DeltaX * row), startY + (res.ExtendedData.DeltaY * col)));
                                    //moduleGrid.Children.Add(textGeometry);
                                }
                            }

                            var transGroup = new TransformGroup();

                            transGroup.Children.Add(new RotateTransform(
                                sec.orientation,
                                res.ExtendedData.DeltaX * (res.ExtendedData.NumColumns + (qzX * 2)) / 2,
                                res.ExtendedData.DeltaY * (res.ExtendedData.NumRows + (qzY * 2)) / 2));

                            transGroup.Children.Add(new TranslateTransform(sec.left, sec.top));

                            //transGroup.Children.Add(new TranslateTransform (res.ExtendedData.Xnw - dX + 1 - (qzX * res.ExtendedData.DeltaX), res.ExtendedData.Ynw - dY + 1 - (qzY * res.ExtendedData.DeltaY)));
                            if (sec.orientation == 0)
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));

                            //works for dataMatrix
                            //if (sec.orientation == 90)
                            //    transGroup.Children.Add(new TranslateTransform(
                            //         sec.width - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1, 
                            //         res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));

                            if (sec.orientation == 90)
                            {
                                var x = sec.symbology == "dataMatrix"
                                    ? sec.width - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1
                                    : sec.width - res.ExtendedData.Ynw - dY - ((res.ExtendedData.NumColumns + qzY) * res.ExtendedData.DeltaY);
                                transGroup.Children.Add(new TranslateTransform(
                                     x,
                                     res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));
                            }

                            if (sec.orientation == 180)
                            {
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));
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
    private object V275DeserializeSector(JObject reportSec, bool removeGS1Data)
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
}
