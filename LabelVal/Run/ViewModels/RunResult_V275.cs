using LabelVal.Sectors.Classes;
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

        if (StoredImageResultGroup == null)
            return;

        SourceImage = StoredImageResultGroup.V275Result.Source;
        V275StoredImage = StoredImageResultGroup.V275Result.Stored;

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(StoredImageResultGroup.V275Result.Report) && !string.IsNullOrEmpty(StoredImageResultGroup.V275Result.Template))
        {
            foreach (JToken jSec in StoredImageResultGroup.V275Result._Job["sectors"])
            {
                foreach (JToken rSec in StoredImageResultGroup.V275Result._Report["inspectLabel"]["inspectSector"])
                {
                    if (jSec["name"].ToString() == rSec["name"].ToString())
                    {

                        tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, (JObject)rSec, RunEntry.GradingStandard, RunEntry.Gs1TableName, StoredImageResultGroup.V275Result._Job["jobVersion"].ToString()));

                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V275StoredSectors.Add(sec);

            V275StoredImageOverlay = V275CreateSectorsImageOverlay(StoredImageResultGroup.V275Result._Job, false, StoredImageResultGroup.V275Result._Report, V275StoredImage, V275StoredSectors);
        }
    }
    private void V275LoadCurrent()
    {
        V275CurrentSectors.Clear();

        if (CurrentImageResultGroup == null)
            return;

        V275CurrentImage = CurrentImageResultGroup.V275Result.Stored;

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(CurrentImageResultGroup.V275Result.Report) && !string.IsNullOrEmpty(CurrentImageResultGroup.V275Result.Template))
        {
            foreach (JToken jSec in CurrentImageResultGroup.V275Result._Job["sectors"])
            {
                foreach (JToken rSec in CurrentImageResultGroup.V275Result._Report["inspectLabel"]["inspectSector"])
                {
                    if (jSec["name"].ToString() == rSec["name"].ToString())
                    {
                        tempSectors.Add(new V275.Sectors.Sector((JObject)jSec, (JObject)rSec, RunEntry.GradingStandard, RunEntry.Gs1TableName, CurrentImageResultGroup.V275Result._Job["jobVersion"].ToString()));
                        break;
                    }
                }
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V275CurrentSectors.Add(sec);

            V275CurrentImageOverlay = V275CreateSectorsImageOverlay(CurrentImageResultGroup.V275Result._Job, false, CurrentImageResultGroup.V275Result._Report, V275CurrentImage, V275CurrentSectors);
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
        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in V275CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
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
                            SectorMissingText = $"Stored Sector {sec.Template.SymbologyType} : Current Sector {cSec.Template.SymbologyType}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sectors.Interfaces.ISector sec in V275StoredSectors)
        {
            bool found = false;
            foreach (Sectors.Interfaces.ISector cSec in V275CurrentSectors)
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
            foreach (Sectors.Interfaces.ISector sec in V275CurrentSectors)
            {
                bool found = false;
                foreach (Sectors.Interfaces.ISector cSec in V275StoredSectors)
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
        foreach (SectorDifferences d in diff)
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

        foreach (JToken jSec in template["sectors"])
        {
            foreach (JObject rSec in report["inspectLabel"]["inspectSector"])
            {
                if (jSec["name"].ToString() == rSec["name"].ToString())
                {
                    if (rSec["type"].ToString() is "blemish" or "ocr" or "ocv")
                        continue;

                    JObject fSec = JsonConvert.DeserializeObject<JObject>(rSec["data"].ToString());
                    JObject result = JsonConvert.DeserializeObject<JObject>(fSec["overallGrade"].ToString());

                    GeometryDrawing sector = new()
                    {
                        Geometry = new RectangleGeometry(new Rect(rSec["left"].Value<double>(), rSec["top"].Value<double>(), rSec["width"].Value<double>(), rSec["height"].Value<double>())),
                        Pen = new Pen(GetGradeBrush(result["grade"]?["letter"].ToString()), 5)
                    };
                    drwGroup.Children.Add(sector);

                    drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun(jSec["username"].ToString(), new Typeface("Arial"), 30.0, new Point(jSec["left"].Value<int>() - 8, jSec["top"].Value<int>() - 8))));

                    double y = rSec["top"].Value<double>() + (rSec["height"].Value<double>() / 2);
                    double x = rSec["left"].Value<double>() + (rSec["width"].Value<double>() / 2);
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

        foreach (JToken sec in template["sectors"])
        {
            Sectors.Interfaces.ISector sect = parsedSectors.FirstOrDefault(e => e.Template.Name.Equals(sec["name"].ToString()));

            if (sect != null)
            {
                GeometryGroup secArea = new();
                secArea.Children.Add(new RectangleGeometry(new Rect(sec["left"].Value<double>(), sec["top"].Value<double>(), sec["width"].Value<double>(), sec["height"].Value<double>())));

                if (sec["symbology"].ToString() is "qr" or "dataMatrix")
                {
                    Sectors.Interfaces.ISectorReport res = sect.Report;

                    if (res.ExtendedData != null)
                    {
                        if (res.ExtendedData.ModuleReflectance != null)
                        {
                            GeometryGroup moduleGrid = new();
                            DrawingGroup textGrp = new();

                            int qzX = (sec["symbology"].ToString() == "dataMatrix") ? 1 : res.ExtendedData.QuietZone;
                            int qzY = res.ExtendedData.QuietZone;

                            double dX = (sec["symbology"].ToString() == "dataMatrix") ? 0 : (res.ExtendedData.DeltaX / 2);
                            double dY = (sec["symbology"].ToString() == "dataMatrix") ? (res.ExtendedData.DeltaY * res.ExtendedData.NumRows) : (res.ExtendedData.DeltaY / 2);

                            int startX = 0;
                            int startY = 0;

                            int cnt = 0;

                            for (int row = -qzX; row < res.ExtendedData.NumRows + qzX; row++)
                            {
                                for (int col = -qzY; col < res.ExtendedData.NumColumns + qzY; col++)
                                {
                                    RectangleGeometry area1 = new(new Rect(startX + (res.ExtendedData.DeltaX * (col + qzX)), startY + (res.ExtendedData.DeltaY * (row + qzY)), res.ExtendedData.DeltaX, res.ExtendedData.DeltaY));
                                    moduleGrid.Children.Add(area1);

                                    string text = res.ExtendedData.ModuleModulation[cnt].ToString();
                                    Typeface typeface = new("Arial");
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

                                        GlyphRun gr = new(_glyphTypeface, 0, false, 2, 1.0f, _glyphIndexes,
                                            new Point(startX + (res.ExtendedData.DeltaX * (col + qzX)) + 1,
                                            startY + (res.ExtendedData.DeltaY * (row + qzY)) + (_glyphTypeface.Height * (res.ExtendedData.DeltaY / 4))),
                                            _advanceWidths, null, null, null, null, null, null);

                                        GlyphRunDrawing grd = new(Brushes.Blue, gr);

                                        textGrp.Children.Add(grd);
                                    }

                                    text = res.ExtendedData.ModuleReflectance[cnt++].ToString();
                                    Typeface typeface1 = new("Arial");
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
                                double x = sec["symbology"].ToString() == "dataMatrix"
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
