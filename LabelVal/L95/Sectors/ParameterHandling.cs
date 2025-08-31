using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO.ParameterTypes;
using BarcodeVerification.lib.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.L95.Sectors
{
    public static class ParameterHandling
    {
        public static void AddParameter(Devices device, Parameters parameter, Symbologies symbology, ICollection<IParameterValue> target, JObject report)
        {
            Type type = parameter.GetDataType(device, symbology);

            if (type == typeof(GradeValue) || type == typeof(Grade))
            {
                IParameterValue gradeValue = GetGradeValueOrGrade(device, parameter, symbology, report.GetParameter<string>(parameter.GetPath(device, symbology)));

                if (gradeValue != null)
                {
                    target.Add(gradeValue);
                    return;
                }
            }
            else if (type == typeof(ValueDouble))
            {
                ValueDouble valueDouble = GetValueDouble(device, parameter, symbology, report.GetParameter<string>(parameter.GetPath(device, symbology)));
                if (valueDouble != null)
                {
                    target.Add(valueDouble);
                    return;
                }
            }
            else if (type == typeof(ValueString))
            {
                ValueString valueString = GetValueString(device, parameter, symbology, report.GetParameter<string>(parameter.GetPath(device, symbology)));
                if (valueString != null)
                {
                    target.Add(valueString); return;
                }
            }
            else if (type == typeof(PassFail))
            {
                PassFail passFail = GetPassFail(device, parameter, symbology, report.GetParameter<string>(parameter.GetPath(device, symbology)));
                if (passFail != null) { target.Add(passFail); return; }
            }
            else if (type == typeof(Custom))
            {

            }

            target.Add(new Missing(parameter));
            Logger.LogDebug($"Paramter: '{parameter}' @ Path: '{parameter.GetPath(device, symbology)}' missing or parse issue.");
        }

        private static IParameterValue GetGradeValueOrGrade(Devices device, Parameters parameter, Symbologies symbology, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            var spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return spl2.Length == 2
                ? new GradeValue(parameter, device, symbology, spl2[0], spl2[1])
                : spl2.Length == 1 ? new Grade(parameter, device, data) : (IParameterValue)null;
        }
        private static ValueDouble GetValueDouble(Devices device, Parameters parameter, Symbologies symbology, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueDouble(parameter, device, symbology, data);
        private static ValueString GetValueString(Devices device, Parameters parameter, Symbologies symbology, string data) => string.IsNullOrWhiteSpace(data) ? null : new ValueString(parameter, device, data);
        private static PassFail GetPassFail(Devices device, Parameters parameter, Symbologies symbology, string data) => string.IsNullOrWhiteSpace(data) ? null : new PassFail(parameter, device, data);
    }
}
