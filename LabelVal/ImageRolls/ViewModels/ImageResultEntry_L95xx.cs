using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using LabelVal.Sectors.ViewModels;
using LabelVal.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResultEntry : ObservableRecipient, IRecipient<PropertyChangedMessage<LabelVal.LVS_95xx.ViewModels.VerifierPacket>>
{
    public class L95xxReport
    {
        public Template Template { get; set; }
        public string Report { get; set; }
    }

    [ObservableProperty] private Databases.ImageResults.L95xxResult l95xxResultRow;

    public List<L95xxReport> L95xxCurrentReport { get; private set; }

    public ObservableCollection<Sectors.ViewModels.Sector> L95xxCurrentSectors { get; } = [];
    public ObservableCollection<Sectors.ViewModels.Sector> L95xxStoredSectors { get; }= [];
    public ObservableCollection<Sectors.ViewModels.SectorDifferences> L95xxDiffSectors { get; }= [];

    //[ObservableProperty] private byte[] l95xxImage = null;
    //[ObservableProperty] private DrawingImage l95xxSectorsImageOverlay;
    //[ObservableProperty] private bool isL95xxImageStored;

    [ObservableProperty] private bool isL95xxWorking = false;
    partial void OnIsL95xxWorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotL95xxWorking));
    public bool IsNotL95xxWorking => !IsL95xxWorking;


    [ObservableProperty] private bool isL95xxFaulted = false;
    partial void OnIsL95xxFaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotL95xxFaulted));
    public bool IsNotL95xxFaulted => !IsL95xxFaulted;

    public void Receive(PropertyChangedMessage<LabelVal.LVS_95xx.ViewModels.VerifierPacket> message) { if (SelectedSector != null) App.Current.Dispatcher.BeginInvoke(() => 
        L95xxCurrentSectors.Add(new Sectors.ViewModels.Sector(SelectedSector.Template, message.NewValue.Value, SelectedSector.DesiredStandard, SelectedSector.DesiredGS1Table))); }


    private void L95xxGetStored()
    {
        L95xxResultRow = SelectedDatabase.Select_L95xxResult(SelectedImageRoll.UID, SourceImage.UID);

        if (L95xxResultRow == null)
        {
            L95xxStoredSectors.Clear();
            return;
        }

        var report = JsonConvert.DeserializeObject<List<L95xxReport>>(L95xxResultRow.Report);

        L95xxStoredSectors.Clear();
        List<Sectors.ViewModels.Sector> tempSectors = [];
        foreach (var rSec in report)
            tempSectors.Add(new Sectors.ViewModels.Sector(rSec.Template, rSec.Report, StandardsTypes.None, GS1TableNames.None));
        

        if (tempSectors.Count > 0) 
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                L95xxStoredSectors.Add(sec);
        }
    }

    private void L95xxGetSectorDiff()
    {
        L95xxDiffSectors.Clear();

        List<Sectors.ViewModels.SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in L95xxStoredSectors)
        {
            foreach (var cSec in L95xxCurrentSectors)
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
        foreach (var sec in L95xxStoredSectors)
        {
            var found = false;
            foreach (var cSec in L95xxCurrentSectors)
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
        if (L95xxStoredSectors.Count > 0)
            foreach (var sec in L95xxCurrentSectors)
            {
                var found = false;
                foreach (var cSec in L95xxStoredSectors)
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
            if (d.IsNotEmpty)
                L95xxDiffSectors.Add(d);
    }
    //public int L95xxLoadTask()
    //{
    //    return 1;
    //}
    //private DrawingImage L95xxCreateSectorsImageOverlay(bool useStored)
    //{
    //    var bmp = ImageUtilities.CreateBitmap(L95xxImage);

    //    //Draw the image outline the same size as the stored image
    //    var border = new GeometryDrawing
    //    {
    //        Geometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
    //        Pen = new Pen(Brushes.Transparent, 1)
    //    };

    //    var secAreas = new GeometryGroup();
    //    var bndAreas = new GeometryGroup();

    //    var drwGroup = new DrawingGroup();

    //    if (useStored)
    //    {
    //        foreach (var sec in L95xxStoredReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in L95xxStoredReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));


    //    }
    //    else
    //    {
    //        foreach (var sec in L95xxCurrentReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in L95xxCurrentReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));

    //    }

    //    var sectors = new GeometryDrawing
    //    {
    //        Geometry = secAreas,
    //        Pen = new Pen(Brushes.Red, 5)
    //    };


    //    var bounding = new GeometryDrawing
    //    {
    //        Geometry = bndAreas,
    //        Pen = new Pen(Brushes.Purple, 5)
    //    };

    //    drwGroup.Children.Add(bounding);
    //    drwGroup.Children.Add(sectors);
    //    drwGroup.Children.Add(border);

    //    var geometryImage = new DrawingImage(drwGroup);
    //    geometryImage.Freeze();
    //    return geometryImage;
    //}
}
