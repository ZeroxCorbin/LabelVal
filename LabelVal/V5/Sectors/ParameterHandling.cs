using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using V5_REST_Lib.Models;

namespace LabelVal.V5.Sectors;

public static class ParamterHandling
{
    public static void AddParameter(Parameters parameter, Symbologies symbology, ObservableCollection<IParameterValue> target, JObject report, JObject template)
    {
        var type = parameter.GetDataType(Devices.V5, symbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            var gradeValue = GetGradeValue(parameter, symbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V5, symbology)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            var valueDouble = GetValueDouble(parameter, symbology, report.GetParameter<string>(parameter.GetPath(Devices.V5, symbology)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            var valueString = GetValueString(parameter, symbology, report.GetParameter<string>(parameter.GetPath(Devices.V5, symbology)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            var passFail = GetPassFail(parameter, symbology, report.GetParameter<string>(parameter.GetPath(Devices.V5, symbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            var valuePassFail = GetValuePassFail(parameter, symbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V5, symbology)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(Custom))
        {
            if (parameter is BarcodeVerification.lib.Common.Parameters.CellSize or BarcodeVerification.lib.Common.Parameters.CellWidth or BarcodeVerification.lib.Common.Parameters.CellHeight)
            {
                ValueDouble valueDouble = new(parameter, Devices.V5, symbology, GetValueDouble(Parameters.Xdim, symbology, report.GetParameter<string>(parameter.GetPath(Devices.V5, symbology))).Value);
                if (valueDouble != null)
                {
                    target.Add(valueDouble);
                    return;
                }
            }

            //if (parameter is AvailableParameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V5, symbology, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is AvailableParameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V5, symbology, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Devices.V5, symbology)}' missing or parse issue.");
    }

    private static IParameterValue GetGradeValue(Parameters parameter, Symbologies symbology, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        var value = gradeValue.GetParameter<string>("value");
        Grade grade = new(parameter, Devices.V5, gradeValue.GetParameter<string>("grade"), V5GetGradeLetter(gradeValue.GetParameter<int>("letter")));
        return string.IsNullOrWhiteSpace(value)
            ? grade
            : new GradeValue(parameter, Devices.V5, symbology, grade, value.ParseDouble());
    }

    private static ValueDouble GetValueDouble(Parameters parameter, Symbologies symbology, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Devices.V5, symbology, value);

    private static ValueString GetValueString(Parameters parameter, Symbologies symbology, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Devices.V5, value);

    private static PassFail GetPassFail(Parameters parameter, Symbologies symbology, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Devices.V5, value);

    public static ValuePassFail GetValuePassFail(Parameters parameter, Symbologies symbology, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        var passFail = valuePassFail["result"].ToString();
        var val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Devices.V5, symbology, val, passFail);
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
