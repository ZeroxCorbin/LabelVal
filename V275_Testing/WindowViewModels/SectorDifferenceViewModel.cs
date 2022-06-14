using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.Utilities;
using V275_Testing.V275.Models;

namespace V275_Testing.WindowViewModels
{
    public class SectorDifferenceViewModel : Core.BaseViewModel
    {
        private string userName;
        public string UserName { get => userName; set => SetProperty(ref userName, value); }

        private string type;
        public string Type { get => type; set => SetProperty(ref type, value); }

        private bool isSectorMissing;
        public bool IsSectorMissing { get => isSectorMissing; set => SetProperty(ref isSectorMissing, value); }

        private bool isNotEmpty = false;
        public bool IsNotEmpty { get => isNotEmpty; set => SetProperty(ref isNotEmpty, value); }

        private ObservableDictionary<string, V275_Report_InspectSector_Common.GradeValue> gradeValues = new ObservableDictionary<string, V275_Report_InspectSector_Common.GradeValue>();
        public ObservableDictionary<string, V275_Report_InspectSector_Common.GradeValue> GradeValues { get => gradeValues; set => SetProperty(ref gradeValues, value); }

        private ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult> valueResults = new ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult>();
        public ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult> ValueResults { get => valueResults; set => SetProperty(ref valueResults, value); }

        private ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult> gs1ValueResults = new ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult>();
        public ObservableDictionary<string, V275_Report_InspectSector_Common.ValueResult> Gs1ValueResults { get => gs1ValueResults; set => SetProperty(ref gs1ValueResults, value); }

        private ObservableDictionary<string, V275_Report_InspectSector_Common.Grade> gs1Grades = new ObservableDictionary<string, V275_Report_InspectSector_Common.Grade>();
        public ObservableDictionary<string, V275_Report_InspectSector_Common.Grade> Gs1Grades { get => gs1Grades; set => SetProperty(ref gs1Grades, value); }

        private ObservableDictionary<string, V275_Report_InspectSector_Common.Value> values = new ObservableDictionary<string, V275_Report_InspectSector_Common.Value>();
        public ObservableDictionary<string, V275_Report_InspectSector_Common.Value> Values { get => values; set => SetProperty(ref values, value); }

        private ObservableCollection<V275_Report_InspectSector_Common.Alarm> alarms = new ObservableCollection<V275_Report_InspectSector_Common.Alarm>();
        public ObservableCollection<V275_Report_InspectSector_Common.Alarm> Alarms { get => alarms; set => SetProperty(ref alarms, value); }

        public void Process(object verify, string userName)
        {
            UserName = userName;
            IsNotEmpty = false;

            foreach (var prop in verify.GetType().GetProperties())
            {
                if (prop.Name == "type")
                    Type = prop.GetValue(verify).ToString();

                if (prop.Name == "data")
                    foreach (var prop1 in prop.GetValue(verify).GetType().GetProperties())
                    {
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Decode))
                        {
                            var decode = (V275_Report_InspectSector_Common.Decode)prop1.GetValue(prop.GetValue(verify));
                            GradeValues.Add(prop1.Name, new V275_Report_InspectSector_Common.GradeValue() { grade = decode.grade, value = decode.value });
                            IsNotEmpty = true;

                            if (Type == "verify1D")
                                ValueResults.Add("edgeDetermination", decode.edgeDetermination);
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.GradeValue))
                        {
                            GradeValues.Add(prop1.Name, (V275_Report_InspectSector_Common.GradeValue)prop1.GetValue(prop.GetValue(verify)));
                            IsNotEmpty = true;
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                        {
                            ValueResults.Add(prop1.Name, (V275_Report_InspectSector_Common.ValueResult)prop1.GetValue(prop.GetValue(verify)));
                            IsNotEmpty = true;
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Value))
                        {
                            Values.Add(prop1.Name, (V275_Report_InspectSector_Common.Value)prop1.GetValue(prop.GetValue(verify)));
                            IsNotEmpty = true;
                            continue;
                        }
                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Alarm[]))
                        {
                            var lst = ((V275_Report_InspectSector_Common.Alarm[])prop1.GetValue(prop.GetValue(verify))).ToList();
                            foreach(var alm in lst)
                                Alarms.Add(alm);
                            //Alarms = ((V275_Report_InspectSector_Common.Alarm[])prop1.GetValue(prop.GetValue(verify))).ToList();
                            IsNotEmpty = true;
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Verify1D.Gs1symbolquality) || prop1.PropertyType == typeof(V275_Report_InspectSector_Verify2D.Gs1symbolquality))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) != null)
                                foreach (var prop2 in prop1.GetValue(prop.GetValue(verify)).GetType().GetProperties())
                                {
                                    if (prop2.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                                    {
                                        Gs1ValueResults.Add(prop2.Name, (V275_Report_InspectSector_Common.ValueResult)prop2.GetValue(prop1.GetValue(prop.GetValue(verify))));
                                        IsNotEmpty = true;
                                        continue;
                                    }
                                    if (prop2.PropertyType == typeof(V275_Report_InspectSector_Common.Grade))
                                    {
                                        Gs1Grades.Add(prop2.Name, (V275_Report_InspectSector_Common.Grade)prop2.GetValue(prop1.GetValue(prop.GetValue(verify))));


                                        if (Gs1Grades[prop2.Name] == null)
                                        {
                                            Gs1Grades.Remove(prop2.Name);
                                            continue;
                                        }

                                        IsNotEmpty = true;
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

        public SectorDifferenceViewModel Compare(SectorDifferenceViewModel compare)
        {
            SectorDifferenceViewModel results = new SectorDifferenceViewModel
            {
                UserName = UserName,
                Type = Type
            };

            foreach (KeyValuePair<string, V275_Report_InspectSector_Common.GradeValue> gv in GradeValues)
                if (compare.GradeValues.ContainsKey(gv.Key.ToString()))
                {
                    if (!CompareGradeValue(gv.Value, compare.GradeValues[gv.Key]))
                    {
                        results.GradeValues.Add(gv.Key, compare.GradeValues[gv.Key]);
                        results.IsNotEmpty = true;
                    }

                }

            foreach (KeyValuePair<string, V275_Report_InspectSector_Common.ValueResult> vr in ValueResults)
                if (compare.ValueResults.ContainsKey(vr.Key))
                {
                    if (!CompareValueResult(vr.Value, compare.ValueResults[vr.Key]))
                    {
                        results.ValueResults.Add(vr.Key, compare.ValueResults[vr.Key]);
                        results.IsNotEmpty = true;
                    }
                }

            foreach (KeyValuePair<string, V275_Report_InspectSector_Common.Value> v in Values)
                if (compare.Values.ContainsKey(v.Key))
                {
                    if (!CompareValue(v.Value, compare.Values[v.Key]))
                    {
                        results.Values.Add(v.Key, compare.Values[v.Key]);
                        results.IsNotEmpty = true;
                    }
                }

            foreach (KeyValuePair<string, V275_Report_InspectSector_Common.ValueResult> v in Gs1ValueResults)
                if (compare.Gs1ValueResults.ContainsKey(v.Key))
                {
                    if (!CompareValueResult(v.Value, compare.Gs1ValueResults[v.Key]))
                    {
                        results.Gs1ValueResults.Add(v.Key, compare.Gs1ValueResults[v.Key]);
                        results.IsNotEmpty = true;
                    }
                }

            foreach (KeyValuePair<string, V275_Report_InspectSector_Common.Grade> v in Gs1Grades)
                if (compare.Gs1Grades.ContainsKey(v.Key))
                {
                    if (!CompareGrade(v.Value, compare.Gs1Grades[v.Key]))
                    {
                        results.Gs1Grades.Add(v.Key, compare.Gs1Grades[v.Key]);
                        results.IsNotEmpty = true;
                    }
                }

            foreach (var aS in compare.Alarms)
            {
                bool found = false;
                foreach (var aC in Alarms)
                {
                    if (aS.name == aC.name)
                    {
                        found = true;
                        if (!CompareAlarm(aS, aC))
                        {
                            results.Alarms.Add(aC);
                            results.IsNotEmpty = true;
                        }
                    }
                }

                if (!found)
                {
                    results.Alarms.Add(aS);
                    results.IsNotEmpty = true;
                }
            }

            return results;
        }

        private bool CompareGrade(V275_Report_InspectSector_Common.Grade source, V275_Report_InspectSector_Common.Grade compare)
        {
            return source.letter == compare.letter;
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
            return (compare.value <= source.value + 5) && (compare.value >= source.value - 5);
        }
        private bool CompareAlarm(V275_Report_InspectSector_Common.Alarm source, V275_Report_InspectSector_Common.Alarm compare)
        {
            if (source.category != compare.category)
                return false;

            if (source.data.subAlarm != compare.data.subAlarm)
                return false;

            return true;

        }
    }
}
