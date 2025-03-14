using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.ISO;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json.Linq;

namespace LabelVal.Sectors.Extensions;

public static class JObjectExtensions
{
    public static string GetParameter(this JObject report, AvailableParameters parameter, AvailableDevices device, AvailableSymbologies symbology)
    {
        string path = parameter.GetParameterPath(device, symbology);
        return report.GetParameter(path);
    }

    public static string GetParameter(this JObject report, string path)
    {
        string[] parts = path.Split('.');
        JObject current = report;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Contains('['))
            {
                //Get the index from the path
                //The index can be either a number or a string
                //Example: //grading[standard].parameters[name:decode]

                //If the index incudes a colon, iterate over the array and find the object that has the key value pair key:value
                string index = parts[i].Substring(parts[i].IndexOf('[') + 1, parts[i].IndexOf(']') - parts[i].IndexOf('[') - 1);

                if (index.Contains(':'))
                {
                    string[] keyValue = index.Split(':');
                    var tmp = parts[i][..parts[i].IndexOf('[')];

                    JArray array = (JArray)current[tmp];
                    for (int j = 0; j < array.Count; j++)
                    {
                        if(array[j] is null || !array[j].HasValues)
                            return null;

                        //Still more to go, but the next object is not a JObject
                        if (i < parts.Length - 1 && array[j][keyValue[0]] is not JObject)
                            return null;
                        else if(i >= parts.Length - 1 && array[j][keyValue[0]].ToString() == keyValue[1])
                        {
                            return array[j].ToString();
                        }
                        else if(i >= parts.Length - 1)
                        {
                            continue;
                        }

                        if (array[j][keyValue[0]].ToString() == keyValue[1])
                        {
                            current = (JObject)array[j];
                            break;
                        }
                    }

                    continue;
                }

                //if the index is a number, we can parse it and get the value from the array
                if (int.TryParse(index, out int idx))
                {
                    current = (JObject)((JArray)current[parts[i][..parts[i].IndexOf('[')]])[idx];
                    continue;
                }

                //if the index is a string, we can get the value from the object
                current = (JObject)current[parts[i][..parts[i].IndexOf('[')]];
var value  = current[index].ToString();
                current = current[value] as JObject;
                continue;
            }

            if (current[parts[i]] is null)
                return null;

            if (i == parts.Length - 1)
                return current[parts[i]].ToString();

            if (current[parts[i]] is JObject)
                current = (JObject)current[parts[i]];
            else
                return null;
        }
        return null;
    }

    public static bool FallsWithin(ISector sector, System.Drawing.Point point) =>
        point.X >= sector.Template.Left
            && point.X <= sector.Template.Left + sector.Template.Width &&
            point.Y >= sector.Template.Top
            && point.Y <= sector.Template.Top + sector.Template.Height;

}
