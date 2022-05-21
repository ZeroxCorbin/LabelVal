using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report_InspectSector_Compare
    {
        public string name { get; set; }
        public string type { get; set; }

        public bool IsSectorMissing { get; set; } = false;

        public Dictionary<string, V275_Report_InspectSector_Common.GradeValue> GradeValues { get; set; } = new Dictionary<string, V275_Report_InspectSector_Common.GradeValue>();
        public Dictionary<string, V275_Report_InspectSector_Common.ValueResult> ValueResults { get; set; } = new Dictionary<string, V275_Report_InspectSector_Common.ValueResult>();
        public Dictionary<string, V275_Report_InspectSector_Common.ValueResult> Gs1ValueResults { get; set; } = new Dictionary<string, V275_Report_InspectSector_Common.ValueResult>();
        public Dictionary<string, V275_Report_InspectSector_Common.Value> Values { get; set; } = new Dictionary<string, V275_Report_InspectSector_Common.Value>();
        public List<V275_Report_InspectSector_Common.Alarm> Alarms { get; set; } = new List<V275_Report_InspectSector_Common.Alarm>();

        public void Process(object verify)
        {
            
            foreach(var prop in verify.GetType().GetProperties())
            {
                if(prop.Name == "name")
                    name = prop.GetValue(verify).ToString();
                if(prop.Name == "type")
                   type = prop.GetValue(verify).ToString();

                if(prop.Name=="data")
                    foreach(var prop1 in prop.GetValue(verify).GetType().GetProperties())
                    {
                        if(prop1.PropertyType == typeof(V275_Report_InspectSector_Common.GradeValue))
                        {
                            GradeValues.Add(prop1.Name, (V275_Report_InspectSector_Common.GradeValue)prop1.GetValue(prop.GetValue(verify)));
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                        {
                            ValueResults.Add(prop1.Name, (V275_Report_InspectSector_Common.ValueResult)prop1.GetValue(prop.GetValue(verify)));
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Value))
                        {
                            Values.Add(prop1.Name, (V275_Report_InspectSector_Common.Value)prop1.GetValue(prop.GetValue(verify)));
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Alarm[]))
                        {
                            Alarms = ((V275_Report_InspectSector_Common.Alarm[])prop1.GetValue(prop.GetValue(verify))).ToList();
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Verify1D.Gs1symbolquality))
                        {
                            foreach(var prop2 in prop1.GetValue(prop.GetValue(verify)).GetType().GetProperties())
                            {
                                if (prop2.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                                {
                                    Gs1ValueResults.Add(prop2.Name, (V275_Report_InspectSector_Common.ValueResult)prop2.GetValue(prop1.GetValue(prop.GetValue(verify))));
                                    continue;
                                }
                            }
                            //ValueResults.Add(prop1.Name, (V275_Report_InspectSector_Common.ValueResult)prop1.GetValue(prop.GetValue(verify)));
                            continue;
                        }

                    }

            }
        }

        private string FormatName(string name)
        {
            string tmp = string.Concat(name.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            return $"{char.ToUpper(tmp[0])}{tmp.Substring(1)}";
        }

        public V275_Report_InspectSector_Compare Compare(V275_Report_InspectSector_Compare compare)
        {
            V275_Report_InspectSector_Compare results = new V275_Report_InspectSector_Compare();

            results.name = name;
            results.type = type;

            foreach (var gv in GradeValues)
                if (compare.GradeValues.ContainsKey(gv.Key))
                {
                    if (!CompareGradeValue(gv.Value, compare.GradeValues[gv.Key]))
                        results.GradeValues.Add(gv.Key, gv.Value);
                }
                else
                {
                    results.GradeValues.Add(gv.Key, gv.Value);
                }

            foreach (var vr in ValueResults)
                if (compare.ValueResults.ContainsKey(vr.Key))
                {
                    if (!CompareValueResult(vr.Value, compare.ValueResults[vr.Key]))
                        results.ValueResults.Add(vr.Key, vr.Value);
                }
                else
                {
                    results.ValueResults.Add(vr.Key, vr.Value);
                }
            
            foreach (var v in Values)
                if (compare.Values.ContainsKey(v.Key))
                {
                    if (!CompareValue(v.Value, compare.Values[v.Key]))
                        results.Values.Add(v.Key, v.Value);
                }
                else
                {
                    results.Values.Add(v.Key, v.Value);
                }

            foreach (var aS in Alarms)
            {
                bool found = false;
                foreach(var aC in compare.Alarms)
                {
                    if(aS.name == aC.name)
                    {
                        found = true;
                        if (!CompareAlarm(aS, aC))
                        {
                            results.Alarms.Add(aS);
                        }
                    }
                }

                if(!found)
                    results.Alarms.Add(aS);
            }

            return results;
        }

        private bool CompareGradeValue(V275_Report_InspectSector_Common.GradeValue source, V275_Report_InspectSector_Common.GradeValue compare)
        {
            return source.grade.letter == compare.grade.letter;

        }
        private bool CompareValueResult(V275_Report_InspectSector_Common.ValueResult source, V275_Report_InspectSector_Common.ValueResult compare)
        {
            return source.result == compare.result;

        }

        private bool CompareValue(V275_Report_InspectSector_Common.Value source, V275_Report_InspectSector_Common.Value compare)
        {
            return source.value == compare.value;

        }
        private bool CompareAlarm(V275_Report_InspectSector_Common.Alarm source, V275_Report_InspectSector_Common.Alarm compare)
        {
            if(source.category != compare.category)
                return false;

            if (source.data.subAlarm != compare.data.subAlarm)
                return false;

            return true;

        }
    }
}
