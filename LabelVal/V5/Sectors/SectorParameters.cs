using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public partial class SectorDetails : ObservableObject, ISectorParameters
{
    public ISector Sector { get; set; }

    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;

    public ObservableCollection<IParameterValue> Parameters { get; } = [];

    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDifferences Compare(ISectorParameters compare) => SectorDifferences.Compare(this, compare);

    public SectorDetails() { }

    public SectorDetails(ISector sector) => Process(sector);
    public void Process(ISector sector)
    {
        if (sector is not V5.Sectors.Sector sec)
            return;

        Sector = sector;
        JObject report = (JObject)Sector.Report.Original;
        JObject template = (JObject)Sector.Template.Original;

        if (Sector.Report.SymbolType == Symbologies.Unknown)
        {
            if (!report.GetParameter<bool>("read"))
            {
                Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, "Read failed"));
            }

            IsSectorMissing = true;
            SectorMissingText = "Sector is missing";
            return;
        }
        //Get thew symbology enum
        Symbologies theSymbology = Sector.Report.SymbolType;

        //Get the region type for the symbology
        AvailableRegionTypes theRegionType = theSymbology.GetSymbologyRegionType(Sector.Report.Device);

        //Get the parameters list based on the region type.
        List<Parameters> theParamters = [.. BarcodeVerification.lib.Common.Parameters.DeviceParameters[(theRegionType, Sector.Report.Device)]];

        foreach (Parameters parameter in theParamters)
        {
            try
            {
                AddParameter(parameter, theSymbology, Parameters, report, template);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Error processing parameter: {parameter}");
            }
        }

        if (sec.Report.GS1Results is null)
            return;

        if (!sec.Report.GS1Results.Valid.Value)
        {
            Alarms.Add(new Alarm(AvaailableAlarmCategories.Error, sec.Report.GS1Results.FormattedOut));
        }

        //if (SymbolType == "verify2D")
        //{
        //    if (results.Datamatrix != null)
        //    {
        //        Values.Add(new Value_("rows", results.Datamatrix.rows));
        //        Values.Add(new Value_("columns", results.Datamatrix.columns));
        //        Values.Add(new Value_("uec", results.Datamatrix.uec));
        //        Values.Add(new Value_("ecc", results.Datamatrix.ecc));
        //        Values.Add(new Value_("mirror", results.Datamatrix.mirror ? 1 : 0));
        //        Values.Add(new Value_("readerConfig", results.Datamatrix.readerConfig ? 1 : 0));
        //    }
        //    else if (results.QR != null)
        //    {
        //        Values.Add(new Value_("rows", results.QR.rows));
        //        Values.Add(new Value_("columns", results.QR.columns));
        //        Values.Add(new Value_("uec", results.QR.uec));
        //        Values.Add(new Value_("mirror", results.QR.mirror ? 1 : 0));
        //        Values.Add(new Value_("model", results.QR.model));
        //        Values.Add(new Value_("locatorCount", results.QR.locator.Count()));
        //    }
        //}
        //else if (SymbolType == "verify1D")
        //{
        //    if (results.Code128 != null)
        //        Values.Add(new Value_("barCount", results.Code128.barCount));
        //    else if (results.PDF417 != null)
        //    {
        //        Values.Add(new Value_("rows", results.PDF417.rows));
        //        Values.Add(new Value_("columns", results.PDF417.columns));
        //        Values.Add(new Value_("ecc", results.PDF417.ecc));
        //    }
        //    else if (results.UPC != null)
        //    {
        //        Values.Add(new Value_("barCount", results.UPC.barCount));
        //        Values.Add(new Value_("supplemental", results.UPC.supplemental));
        //    }
        //}
    }

    private void AddParameter(Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report, JObject template)
    {
        Type type = parameter.GetParameterDataType(Sector.Report.Device, theSymbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            IParameterValue gradeValue = GetGradeValue(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, report.GetParameter<JObject>(parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(OverallGrade))
        {
            target.Add(Sector.Report.OverallGrade);

        }
        else if (type == typeof(Custom))
        {
            if (parameter is BarcodeVerification.lib.Common.Parameters.CellSize or BarcodeVerification.lib.Common.Parameters.CellWidth or BarcodeVerification.lib.Common.Parameters.CellHeight)
            {
                ValueDouble valueDouble = new(parameter, Sector.Report.Device, Sector.Report.SymbolType, Sector.Report.XDimension);
                if (valueDouble != null)
                {
                    target.Add(valueDouble);
                    return;
                }
            }

            //if (parameter is AvailableParameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Sector.Report.Device, Sector.Report.SymbolType, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is AvailableParameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Sector.Report.Device, Sector.Report.SymbolType, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetParameterPath(Sector.Report.Device, Sector.Report.SymbolType)}' missing or parse issue.");
    }

    private IParameterValue GetGradeValue(Parameters parameter, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        string value = gradeValue.GetParameter<string>("value");
        Grade grade = new(parameter, Sector.Report.Device, gradeValue.GetParameter<string>("grade"), V5GetGradeLetter(gradeValue.GetParameter<int>("letter")));
        return string.IsNullOrWhiteSpace(value)
            ? grade
            : new GradeValue(parameter, Sector.Report.Device, Sector.Report.SymbolType, grade, value.ParseDouble());
    }

    private ValueDouble GetValueDouble(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Sector.Report.Device, Sector.Report.SymbolType, value);

    private ValueString GetValueString(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Sector.Report.Device, value);

    private PassFail GetPassFail(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Sector.Report.Device, value);

    public ValuePassFail GetValuePassFail(Parameters parameter, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        string passFail = valuePassFail["result"].ToString();
        string val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Sector.Report.Device, Sector.Report.SymbolType, val, passFail);
    }

    private static string V5GetGradeLetter(int grade) => grade switch
    {
        65 => "A",
        66 => "B",
        67 => "C",
        68 => "D",
        70 => "F",
        _ => "U",
    };

    private static string V5GetSymbolType(ResultsAlt.Decodedata results) => results.Code128 != null
            ? "verify1D"
            : results.Datamatrix != null
            ? "verify2D"
            : results.QR != null ? "verify2D" : results.PDF417 != null ? "verify1D" : results.UPC != null ? "verify1D" : "Unknown";

    private static string GetLetter(double value) =>
value == 4.0f
? "A"
: value is <= 3.9f and >= 3.0f
? "B"
: value is <= 2.9f and >= 2.0f
? "C"
: value is <= 1.9f and >= 1.0f
? "D"
: value is <= 0.9f and >= 0.0f
? "F"
: "F";
}
