using LabelVal.Sectors.Classes;
using Lvs95xx.lib.Hardware.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using V275_REST_Lib.Models;

namespace LabelVal.Run.ViewModels;
public partial class RunResult
{
    private void V275LoadStored()
    {
        V275StoredSectors.Clear();

        if (StoredResultsGroup == null)
            return;

        SourceImage = StoredResultsGroup.V275Result.Source;
        V275StoredImage = StoredResultsGroup.V275Result.Stored;

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(StoredResultsGroup.V275Result.ReportString) && !string.IsNullOrEmpty(StoredResultsGroup.V275Result.TemplateString))
        {
            foreach (var jSec in StoredResultsGroup.V275Result.Template["sectors"])
            {
                foreach (var rSec in StoredResultsGroup.V275Result.Report["inspectLabel"]["inspectSector"])
                {
                    if (jSec["name"].ToString() == rSec["name"].ToString())
                    {

                        tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, (JObject)rSec, [RunEntry.GradingStandard], RunEntry.ApplicationStandard, RunEntry.Gs1TableName, StoredResultsGroup.V275Result.Template["jobVersion"].ToString()));

                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (var sec in tempSectors)
                V275StoredSectors.Add(sec);

            V275StoredImageOverlay = V275CreateSectorsImageOverlay(StoredResultsGroup.V275Result.Template, false, StoredResultsGroup.V275Result.Report, V275StoredImage, V275StoredSectors);
        }
    }
    private void V275LoadCurrent()
    {
        V275CurrentSectors.Clear();

        if (CurrentResultsGroup == null)
            return;

        V275CurrentImage = CurrentResultsGroup.V275Result.Stored;

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(CurrentResultsGroup.V275Result.ReportString) && !string.IsNullOrEmpty(CurrentResultsGroup.V275Result.TemplateString))
        {
            foreach (var jSec in CurrentResultsGroup.V275Result.Template["sectors"])
            {
                foreach (var rSec in CurrentResultsGroup.V275Result.Report["inspectLabel"]["inspectSector"])
                {
                    if (jSec["name"].ToString() == rSec["name"].ToString())
                    {
                        tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, (JObject)rSec, [RunEntry.GradingStandard], RunEntry.ApplicationStandard, RunEntry.Gs1TableName, CurrentResultsGroup.V275Result.Template["jobVersion"].ToString()));
                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (var sec in tempSectors)
                V275CurrentSectors.Add(sec);

            V275CurrentImageOverlay = V275CreateSectorsImageOverlay(CurrentResultsGroup.V275Result.Template, false, CurrentResultsGroup.V275Result.Report, V275CurrentImage, V275CurrentSectors);
        }
    }

    private static object V275DeserializeSector(JObject reportSec, bool removeGS1Data)
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
    private void V275GetSectorDiff()
    {
        V275DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in V275StoredSectors)
        {
            foreach (var cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        diff.Add(sec.SectorDetails.Compare(cSec.SectorDetails));
                        continue;
                    }
                    else
                    {
                        SectorDifferences dat = new()
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Report.Symbology.GetDescription()} : Current Sector {cSec.Report.Symbology.GetDescription()}"
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
                SectorDifferences dat = new()
                {
                    Username = $"{sec.Template.Username} (MISSING)",
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
                    SectorDifferences dat = new()
                    {
                        Username = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        //ToDo: Sort the diff list
        foreach (var d in diff)
            V275DiffSectors.Add(d);

    }

    private DrawingImage V275CreateSectorsImageOverlay(JObject template, bool isDetailed, JObject report, ImageRolls.ViewModels.ImageEntry image, ObservableCollection<Sectors.Interfaces.ISector> sectors)
    {
        DrawingGroup drwGroup = new();

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, image.Image.PixelWidth - 1, image.Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secCenter = new();

        foreach (var jSec in template["sectors"])
        {
            foreach (JObject rSec in report["inspectLabel"]["inspectSector"])
            {
                if (jSec["name"].ToString() == rSec["name"].ToString())
                {
                    if (rSec["type"].ToString() is "blemish" or "ocr" or "ocv")
                        continue;

                    var fSec = JsonConvert.DeserializeObject<JObject>(rSec["data"].ToString());
                    var result = JsonConvert.DeserializeObject<JObject>(fSec["overallGrade"].ToString());

                    GeometryDrawing sector = new()
                    {
                        Geometry = new RectangleGeometry(new Rect(rSec["left"].Value<double>(), rSec["top"].Value<double>(), rSec["width"].Value<double>(), rSec["height"].Value<double>())),
                        Pen = new Pen(GetGradeBrush(result["grade"]?["letter"].ToString()), 5)
                    };
                    drwGroup.Children.Add(sector);

                    drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(jSec["username"].ToString(), new Typeface("Arial"), 30.0, new Point(jSec["left"].Value<int>() - 8, jSec["top"].Value<int>() - 8))));

                    var y = rSec["top"].Value<double>() + (rSec["height"].Value<double>() / 2);
                    var x = rSec["left"].Value<double>() + (rSec["width"].Value<double>() / 2);
                    secCenter.Children.Add(new LineGeometry(new Point(x + 10, y), new Point(x + -10, y)));
                    secCenter.Children.Add(new LineGeometry(new Point(x, y + 10), new Point(x, y + -10)));

                    break;
                }
            }
        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        drwGroup.Children.Add(sectorCenters);

        if (isDetailed)
            drwGroup.Children.Add(V275GetModuleGrid(template, sectors));

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();

        return geometryImage;
    }
    private static DrawingGroup V275GetModuleGrid(JObject template, ObservableCollection<Sectors.Interfaces.ISector> parsedSectors)
    {
        DrawingGroup drwGroup = new();

        foreach (var sec in template["sectors"])
        {
            var sect = parsedSectors.FirstOrDefault(e => e.Template.Name.Equals(sec["name"].ToString()));

            if (sect != null)
            {
                GeometryGroup secArea = new();
                secArea.Children.Add(new RectangleGeometry(new Rect(sec["left"].Value<double>(), sec["top"].Value<double>(), sec["width"].Value<double>(), sec["height"].Value<double>())));

                if (sec["symbology"].ToString() is "qr" or "dataMatrix")
                {
                    var res = sect.Report;

                    if (res.ExtendedData != null)
                    {
                        if (res.ExtendedData.ModuleReflectance != null)
                        {
                            GeometryGroup moduleGrid = new();
                            DrawingGroup textGrp = new();

                            var qzX = (sec["symbology"].ToString() == "dataMatrix") ? 1 : res.ExtendedData.QuietZone;
                            var qzY = res.ExtendedData.QuietZone;

                            var dX = (sec["symbology"].ToString() == "dataMatrix") ? 0 : (res.ExtendedData.DeltaX / 2);
                            var dY = (sec["symbology"].ToString() == "dataMatrix") ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                            var startX = 0;
                            var startY = 0;

                            var cnt = 0;

                            for (var row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                            {
                                for (var col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                                {
                                    RectangleGeometry area1 = new(new Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                                    moduleGrid.Children.Add(area1);

                                    var text = res.ExtendedData.ModuleModulation[cnt].ToString();
                                    Typeface typeface = new("Arial");
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

                                        GlyphRun gr = new(_glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        GlyphRunDrawing grd = new(Brushes.Blue, gr);

                                        textGrp.Children.Add(grd);
                                    }

                                    text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
                                    Typeface typeface1 = new("Arial");
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

                                        GlyphRun gr = new(_glyphTypeface1, 0, false, 2, 1.0f, _glyphIndexes,
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface1.Height * (res.ExtendedData.DeltaY / 2))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        GlyphRunDrawing grd = new(Brushes.Blue, gr);
                                        textGrp.Children.Add(grd);
                                    }
                                }
                            }

                            TransformGroup transGroup = new();

                            transGroup.Children.Add(new RotateTransform(
                                sec["orientation"].Value<double>(),
                                res.ExtendedData.DeltaX * (res.ExtendedData.NumColumns + (qzX * 2)) / 2,
                                res.ExtendedData.DeltaY * (res.ExtendedData.NumRows + (qzY * 2)) / 2));

                            transGroup.Children.Add(new TranslateTransform(sec["left"].Value<double>(), sec["top"].Value<double>()));

                            if (sec["orientation"].Value<double>() == 0)
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));

                            if (sec["orientation"].Value<double>() == 90)
                            {
                                var x = sec["symbology"].ToString() == "dataMatrix"
                                    ? sec["width"].Value<double>() - res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - 1
                                    : sec["width"].Value<double>() - res.ExtendedData.Ynw - dY - ((res.ExtendedData.NumColumns + qzY) * res.ExtendedData.DeltaY);
                                transGroup.Children.Add(new TranslateTransform(
                                     x,
                                     res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1));
                            }

                            if (sec["orientation"].Value<double>() == 180)
                            {
                                transGroup.Children.Add(new TranslateTransform(
                                    res.ExtendedData.Xnw - (qzX * res.ExtendedData.DeltaX) - dX + 1,
                                    res.ExtendedData.Ynw - (qzY * res.ExtendedData.DeltaY) - dY + 1));
                            }

                            moduleGrid.Transform = transGroup;
                            textGrp.Transform = transGroup;

                            GeometryDrawing mGrid = new()
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

        return drwGroup;
    }
}
