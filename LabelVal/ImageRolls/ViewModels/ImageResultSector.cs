using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResultSector : ObservableObject
{
    public class Sector
    {
        public string name { get; set; }
        public string username { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int left { get; set; }
        public int top { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int angle { get; set; }
        public int orientation { get; set; }
        public bool supportMatching { get; set; }
        public Matchsettings matchSettings { get; set; }
        public string symbology { get; set; }
        public float warningGrade { get; set; }
        public float passingGrade { get; set; }
        public int apertureMode { get; set; }
        public int aperturePercent { get; set; }
        public int apertureDimension { get; set; }
        public Gradingstandard gradingStandard { get; set; }
        public string metaData { get; set; }

        //Blemsh
        public int separation { get; set; }
        public int reduction { get; set; }
        public int warningPercent { get; set; }
        public int maxErrorsBeforeQuit { get; set; }
        public int maxThumbnailsPerSector { get; set; }
        public int dilation { get; set; }
        public string unitMeasure { get; set; }
        public Area foreground { get; set; }
        public Area background { get; set; }
        public Area matrix { get; set; }
        public Area dieCut { get; set; }
        public Goldenimage goldenImage { get; set; }

        public Mask blemishMask { get; set; }
    }

    public class Matchsettings
    {
        public int dataLength { get; set; }
        public string fieldMask { get; set; }
        public int mod10CheckDigit { get; set; }
        public int requireFNC1 { get; set; }
        public int matchMode { get; set; }
        public string promptUserAtStartMessage { get; set; }
        public string fixedText { get; set; }
        public string matchToSector { get; set; }
        public int matchSectorStartPosition { get; set; }
        public int stepCharSetOption { get; set; }
        public int stepDelta { get; set; }
        public string stepCharSet { get; set; }
        public int userDefinedDataOption { get; set; }
        public object[] userDefinedData { get; set; }
        public int userDefinedDataTrueSize { get; set; }
        public int duplicateCheckOption { get; set; }
        public int uniqueSetNumber { get; set; }
    }

    public class Gradingstandard
    {
        public bool enabled { get; set; }
        public string standard { get; set; }
        public string tableId { get; set; }
        public Specifications specifications { get; set; }
        public int xdimFailOption { get; set; }
        public int barheightFailOption { get; set; }
    }

    public class Goldenimage
    {
    }

    public class Specifications
    {
        public string symbology { get; set; }
        public string symbolType { get; set; }
        public float minXdim { get; set; }
        public float maxXdim { get; set; }
        public float minHeightFactor { get; set; }
        public float minHeightAbs { get; set; }
        public int minLeftQZ { get; set; }
        public int minRightQZ { get; set; }
        public float minOverallGrade { get; set; }
        public float aperture { get; set; }
    }

    public class Area
    {
        public int sensitivity { get; set; }
        public float maximumDimension { get; set; }
        public float maximumArea { get; set; }
        public bool checkArea { get; set; }
    }

    public class Mask
    {
        public int width { get; set; }
        public int height { get; set; }
        public State[] states { get; set; }
        public Layer[] layers { get; set; }
    }

    public class State
    {
        public string name { get; set; }
        public int value { get; set; }
        public int layer { get; set; }
    }

    public class Layer
    {
        public int value { get; set; }
        public int[] runLengthEncode { get; set; }
    }

    [ObservableProperty] private Sector templateSector;
    [ObservableProperty] private object reportSector;
    [ObservableProperty] private ImageResultSectorDifferences sectorResults = new();

    [ObservableProperty] private bool isWarning;
    [ObservableProperty] private bool isError;

    [ObservableProperty] private bool isGS1Standard;

    [ObservableProperty] private bool isWrongStandard;
    partial void OnIsWrongStandardChanged(bool value) => OnPropertyChanged(nameof(IsNotWrongStandard));
    public bool IsNotWrongStandard => !IsWrongStandard;

    public ImageResultSector() { }
    public ImageResultSector(Sector templateSector, object reportSector, bool isWrongStandard, bool isGS1Standard)
    {
        ReportSector = reportSector;
        TemplateSector = templateSector;

        IsWrongStandard = isWrongStandard;
        IsGS1Standard = isGS1Standard;

        SectorResults.Process(reportSector, TemplateSector.username, IsGS1Standard);

        var highCat = 0;

        foreach (var alm in SectorResults.Alarms)
        {
            //Alarms.Add(alm);
            if (highCat < alm.category)
                highCat = alm.category;
        }

        if (highCat == 1)
            IsWarning = true;
        else if (highCat == 2)
            IsError = true;


    }
}
