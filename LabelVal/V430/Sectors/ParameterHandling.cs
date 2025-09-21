using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace LabelVal.V430.Sectors;

public static class ParameterHandling
{

    private static OverallGrade GetOverallGrade(JObject decode)
    {
        if (decode == null)
        {
            return new OverallGrade(Devices.V430, new Grade(BarcodeVerification.lib.Common.Parameters.OverallGrade, Devices.V430, 0), "0.0/0/0", "0", "0");
        }
        return new OverallGrade(Devices.V430, new Grade(BarcodeVerification.lib.Common.Parameters.OverallGrade, Devices.V430, 4.0), "4.0/0/0", "0", "0");

    }

    public static void AddParameter(BarcodeVerification.lib.Common.Parameters parameter, Symbologies theSymbology, ObservableCollection<IParameterValue> target, JObject report, JObject template, string reportId, string decodeId)
    {
        Type type = parameter.GetDataType(Devices.V430, theSymbology);

        if (type == typeof(GradeValue))
        {
            GradeValue gradeValue = GetGradeValue(parameter, theSymbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V430, theSymbology)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(Grade))
        {
            Grade grade = GetGrade(parameter, report.GetParameter<JObject>(parameter.GetPath(Devices.V430, theSymbology)));

            if (grade != null)
            {
                target.Add(grade);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, theSymbology, report.GetParameter<string>(parameter.GetPath(Devices.V430, theSymbology)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Devices.V430, theSymbology)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetPath(Devices.V430, theSymbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, theSymbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V430, theSymbology)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(OverallGrade))
        {
            var ipReport = report.GetParameter<JObject>($"ipReports[uId:{reportId}]");
            var decode = ipReport.GetParameter<JObject>($"decodes[dId:{decodeId}]");
            if (decode == null)
            {
                 Logger.Debug($"Could not find decode: '{decodeId}' in ReportData. {Devices.V430}");
                return;
            }
            target.Add(GetOverallGrade(decode));
            return;

        }
        else if (type == typeof(Custom))
        {

            //if (parameter is BarcodeVerification.lib.Common.Parameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V430, SymbolType, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        BarcodeVerification.lib.Common.Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is BarcodeVerification.lib.Common.Parameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V430, SymbolType, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        BarcodeVerification.lib.Common.Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.Debug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Devices.V430, theSymbology)}' missing or parse issue.");
    }

    private static GradeValue? GetGradeValue(BarcodeVerification.lib.Common.Parameters parameter, Symbologies symbology, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;

        Grade grade = new(parameter, Devices.V430, gradeValue["grade"].ToString());
        var value = gradeValue["value"].ToString();
        return new GradeValue(parameter, Devices.V430, symbology, grade, value);
    }

    private static Grade? GetGrade(BarcodeVerification.lib.Common.Parameters parameter, JObject grade)
    {
        if (grade is null)
            return null;
        var value = grade["value"].ToString();
        _ = grade["letter"].ToString();
        return new Grade(parameter, Devices.V430, value);
    }

    private static ValueDouble? GetValueDouble(BarcodeVerification.lib.Common.Parameters parameter, Symbologies symbology, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Devices.V430, symbology, value);

    private static ValueString? GetValueString(BarcodeVerification.lib.Common.Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Devices.V430, value);

    private static PassFail? GetPassFail(BarcodeVerification.lib.Common.Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Devices.V430, value);

    public static ValuePassFail? GetValuePassFail(BarcodeVerification.lib.Common.Parameters parameter, Symbologies symbology, JObject valuePassFail)
    {
        if (valuePassFail is null)
            return null;

        var passFail = valuePassFail["result"].ToString();
        var val = valuePassFail["value"].ToString();
        return new ValuePassFail(parameter, Devices.V430, symbology, val, passFail);
    }
}
