using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;
using Newtonsoft.Json.Linq;

namespace LabelVal.V275.Sectors;

public static class ParameterHandling
{
    public static void AddParameter(Parameters parameter, Symbologies symbology, ICollection<IParameterValue> target, JObject report, JObject template)
    {
        Type type = parameter.GetDataType(Devices.V275, symbology);

        if (type == typeof(GradeValue) || type == typeof(Grade))
        {
            IParameterValue gradeValue = GetGradeValueOrGrade(parameter, symbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V275, symbology)));

            if (gradeValue != null)
            {
                target.Add(gradeValue);
                return;
            }
        }
        else if (type == typeof(ValueDouble))
        {
            ValueDouble valueDouble = GetValueDouble(parameter, symbology, report.GetParameter<string>(parameter.GetPath(Devices.V275, symbology)));
            if (valueDouble != null)
            {
                target.Add(valueDouble);
                return;
            }
        }
        else if (type == typeof(ValueString))
        {
            ValueString valueString = parameter is BarcodeVerification.lib.Common.Parameters.GS1Table
                ? GetValueString(parameter, template.GetParameter<string>(parameter.GetPath(Devices.V275, symbology)))
                : GetValueString(parameter, report.GetParameter<string>(parameter.GetPath(Devices.V275, symbology)));
            if (valueString != null) { target.Add(valueString); return; }
        }
        else if (type == typeof(PassFail))
        {
            PassFail passFail = GetPassFail(parameter, report.GetParameter<string>(parameter.GetPath(Devices.V275, symbology)));
            if (passFail != null) { target.Add(passFail); return; }
        }
        else if (type == typeof(ValuePassFail))
        {
            ValuePassFail valuePassFail = GetValuePassFail(parameter, symbology, report.GetParameter<JObject>(parameter.GetPath(Devices.V275, symbology)));
            if (valuePassFail != null) { target.Add(valuePassFail); return; }
        }
        else if (type == typeof(OverallGrade))
        {
            OverallGrade overallGrade = GetOverallGrade(report.GetParameter<JObject>(parameter.GetPath(Devices.V275, symbology)));
            if (overallGrade != null) { target.Add(overallGrade); return; }
        }
        else if (type == typeof(Custom))
        {

            //if (parameter is AvailableParameters.UnusedEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V275, symbology, report.GetParameter<double>("Datamatrix.uec"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}

            //if (parameter is AvailableParameters.MinimumEC)
            //{
            //    ValueDouble valueDouble = new(parameter, Devices.V275, symbology, report.GetParameter<double>("Datamatrix.ecc"));
            //    if (valueDouble != null)
            //    {
            //        Parameters.Add(valueDouble);
            //        continue;
            //    }
            //}
        }

        target.Add(new Missing(parameter));
        Logger.Debug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(Devices.V275, symbology)}' missing or parse issue.");
    }

    public static OverallGrade GetOverallGrade(JObject json)
    {
        var spl = json["string"].ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);

        Grade grade = new(BarcodeVerification.lib.Common.Parameters.OverallGrade, Devices.V275, json["grade"]["value"].Value<double>());
        return new OverallGrade(Devices.V275, grade, json["string"].ToString(), spl[1], spl[2]);
    }
    private static IParameterValue GetGradeValueOrGrade(Parameters parameter, Symbologies symbology, JObject gradeValue)
    {
        if (gradeValue is null)
            return null;
        var value = gradeValue["value"].ToString();
        Grade grade = GetGrade(parameter, (JObject)gradeValue["grade"]);
        return grade == null
            ? new Grade(parameter, Devices.V275, value)
            : new GradeValue(parameter, Devices.V275, symbology, grade, value);
    }
    private static Grade GetGrade(Parameters parameter, JObject gradeValue) => gradeValue is null
            ? null
            : new Grade(parameter, Devices.V275, gradeValue["value"].ToString());

    private static ValueDouble GetValueDouble(Parameters parameter, Symbologies symbology, string value) => string.IsNullOrWhiteSpace(value)
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : new ValueDouble(parameter, Devices.V275, symbology, value);

    private static ValueString GetValueString(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new ValueString(parameter, Devices.V275, value);

    private static PassFail GetPassFail(Parameters parameter, string value) => string.IsNullOrWhiteSpace(value) ? null : new PassFail(parameter, Devices.V275, value);

    public static ValuePassFail GetValuePassFail(Parameters parameter, Symbologies symbology, JObject valuePassFail) => valuePassFail is null
            ? null
            : new ValuePassFail(parameter, Devices.V275, symbology, valuePassFail["value"].ToString(), valuePassFail["result"].ToString());
}
