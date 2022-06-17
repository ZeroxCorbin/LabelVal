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

        private bool isNotOCVMatch = false;
        public bool IsNotOCVMatch { get => isNotOCVMatch; set => SetProperty(ref isNotOCVMatch, value); }

        private string oCVMatchText;
        public string OCVMatchText { get => oCVMatchText; set => SetProperty(ref oCVMatchText, value); }

        private bool isSectorMissing;
        public bool IsSectorMissing { get => isSectorMissing; set => SetProperty(ref isSectorMissing, value); }

        private bool isNotEmpty = false;
        public bool IsNotEmpty { get => isNotEmpty; set => SetProperty(ref isNotEmpty, value); }

        private bool isGS1Standard;
        public bool IsGS1Standard { get => isGS1Standard; set => SetProperty(ref isGS1Standard, value); }

        public class GradeValue : V275_Report_InspectSector_Common.GradeValue
        {
            public string name { get; set; }

            public GradeValue(string name, V275_Report_InspectSector_Common.GradeValue data)
            {
                this.value = data.value;
                this.grade = data.grade;
                this.name = name;
            }
        }

        private ObservableCollection<GradeValue> gradeValues = new ObservableCollection<GradeValue>();
        public ObservableCollection<GradeValue> GradeValues { get => gradeValues; set => SetProperty(ref gradeValues, value); }


        public class ValueResult : V275_Report_InspectSector_Common.ValueResult
        {
            public string name { get; set; }

            public ValueResult(string name, V275_Report_InspectSector_Common.ValueResult data)
            {
                this.value = data.value;
                this.result = data.result;
                this.name = name;
            }
        }

        private ObservableCollection<ValueResult> valueResults = new ObservableCollection<ValueResult>();
        public ObservableCollection<ValueResult> ValueResults { get => valueResults; set => SetProperty(ref valueResults, value); }

        private ObservableCollection<ValueResult> gs1ValueResults = new ObservableCollection<ValueResult>();
        public ObservableCollection<ValueResult> Gs1ValueResults { get => gs1ValueResults; set => SetProperty(ref gs1ValueResults, value); }


        public class Grade : V275_Report_InspectSector_Common.Grade
        {
            public string name { get; set; }

            public Grade(string name, V275_Report_InspectSector_Common.Grade data)
            {
                this.value = data.value;
                this.letter = data.letter;
                this.name = name;
            }
        }

        private ObservableCollection<Grade> gs1Grades = new ObservableCollection<Grade>();
        public ObservableCollection<Grade> Gs1Grades { get => gs1Grades; set => SetProperty(ref gs1Grades, value); }


        public class Value : V275_Report_InspectSector_Common.Value
        {
            public string name { get; set; }

            public Value(string name, V275_Report_InspectSector_Common.Value data)
            {
                this.value = data.value;
                this.name = name;
            }
        }

        private ObservableCollection<Value> values = new ObservableCollection<Value>();
        public ObservableCollection<Value> Values { get => values; set => SetProperty(ref values, value); }


        private ObservableCollection<V275_Report_InspectSector_Common.Alarm> alarms = new ObservableCollection<V275_Report_InspectSector_Common.Alarm>();
        public ObservableCollection<V275_Report_InspectSector_Common.Alarm> Alarms { get => alarms; set => SetProperty(ref alarms, value); }


        public class Blemish : V275_Report_InspectSector_Blemish.Blemish
        {
            public System.Drawing.Rectangle rectangle => new System.Drawing.Rectangle(this.top, this.left, this.width, this.height);

            public Blemish(V275_Report_InspectSector_Blemish.Blemish data)
            {
                top = data.top;
                left = data.left;
                height = data.height;
                width = data.width;
                type = data.type;
            }
        }

        private ObservableCollection<Blemish> blemishes = new ObservableCollection<Blemish>();
        public ObservableCollection<Blemish> Blemishes { get => blemishes; set => SetProperty(ref blemishes, value); }


        public void Process(object verify, string userName, bool isGS1Standard)
        {
            IsGS1Standard = isGS1Standard;

            UserName = userName;
            IsNotEmpty = false;

            foreach (var prop in verify.GetType().GetProperties())
            {
                if (prop.Name == "type")
                    Type = prop.GetValue(verify).ToString();

                if (prop.Name == "data")
                    foreach (var prop1 in prop.GetValue(verify).GetType().GetProperties())
                    {
                        if (Type == "ocr" || Type == "ocv")
                        {
                            if (prop1.Name == "text")
                                OCVMatchText = (string)prop1.GetValue(prop.GetValue(verify));
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Blemish.Blemish[]))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) is V275_Report_InspectSector_Blemish.Blemish[] dat)
                            {
                                foreach (var d in dat)
                                    Blemishes.Add(new Blemish(d));

                                IsNotEmpty = Blemishes.Count > 0;
                            }
                            continue;
                        }


                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Decode))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) is V275_Report_InspectSector_Common.Decode dat)
                            {
                                GradeValues.Add(new GradeValue(prop1.Name, new V275_Report_InspectSector_Common.GradeValue() { grade = dat.grade, value = dat.value }));

                                if (Type == "verify1D")
                                    ValueResults.Add(new ValueResult("edgeDetermination", dat.edgeDetermination));

                                IsNotEmpty = true;
                            }
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.GradeValue))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) is V275_Report_InspectSector_Common.GradeValue dat)
                            {
                                GradeValues.Add(new GradeValue(prop1.Name, dat));
                                IsNotEmpty = true;

                            }
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) is V275_Report_InspectSector_Common.ValueResult dat)
                            {
                                ValueResults.Add(new ValueResult(prop1.Name, dat));
                                IsNotEmpty = true;
                            }
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Value))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) is V275_Report_InspectSector_Common.Value dat)
                            {
                                Values.Add(new Value(prop1.Name, dat));
                                IsNotEmpty = true;
                            }
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Common.Alarm[]))
                        {
                            if ((prop1.GetValue(prop.GetValue(verify))) is V275_Report_InspectSector_Common.Alarm[] dat)
                            {
                                foreach (var d in dat)
                                    Alarms.Add(d);

                                IsNotEmpty = Alarms.Count > 0;
                            }
                            continue;
                        }

                        if (prop1.PropertyType == typeof(V275_Report_InspectSector_Verify1D.Gs1symbolquality) || prop1.PropertyType == typeof(V275_Report_InspectSector_Verify2D.Gs1symbolquality))
                        {
                            if (prop1.GetValue(prop.GetValue(verify)) != null)
                                foreach (var prop2 in prop1.GetValue(prop.GetValue(verify)).GetType().GetProperties())
                                {
                                    if (prop2.PropertyType == typeof(V275_Report_InspectSector_Common.ValueResult))
                                    {
                                        if (prop2.GetValue(prop1.GetValue(prop.GetValue(verify))) is V275_Report_InspectSector_Common.ValueResult dat)
                                        {
                                            Gs1ValueResults.Add(new ValueResult(prop2.Name, dat));
                                            IsNotEmpty = true;
                                        }
                                        continue;
                                    }
                                    if (prop2.PropertyType == typeof(V275_Report_InspectSector_Common.Grade))
                                    {
                                        if (prop2.GetValue(prop1.GetValue(prop.GetValue(verify))) is V275_Report_InspectSector_Common.Grade dat)
                                        {
                                            Gs1Grades.Add(new Grade(prop2.Name, dat));
                                            IsNotEmpty = true;
                                        }
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
                Type = Type,
                IsGS1Standard = IsGS1Standard,
            };

            if (Type == "ocr" || Type == "ocr")
            {
                if (!OCVMatchText.Equals(compare.OCVMatchText))
                {
                    if(compare.OCVMatchText != null)
                    {
                        results.IsNotEmpty = true;
                        results.IsNotOCVMatch = true;
                        results.OCVMatchText = $"{OCVMatchText} / {compare.OCVMatchText}";
                    }
                }
                    
            }

            foreach (Blemish src in Blemishes)
                if (compare.Blemishes.FirstOrDefault((x) => x.rectangle.Contains(new System.Drawing.Point(src.rectangle.Left + (src.width / 2), src.rectangle.Top + (src.rectangle.Height / 2)))) is Blemish cmp)
                {
                    //if (cmp == null)
                    //{
                    //    results.Blemishes.Add(src);
                    //    results.IsNotEmpty = true;
                    //    continue;
                    //}
                        

                    //results.Blemishes.Add(cmp);
                    //results.IsNotEmpty = true;

                }
                else
                {
                    results.Blemishes.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (GradeValue src in GradeValues)
                if (compare.GradeValues.FirstOrDefault((x) => x.name == src.name) is GradeValue cmp)
                {
                    if (cmp == null) continue;

                    if (!CompareGradeValue(src, cmp))
                    {
                        results.GradeValues.Add(cmp);
                        results.IsNotEmpty = true;
                    }
                }
                else
                {
                    results.GradeValues.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (ValueResult src in ValueResults)
                if (compare.ValueResults.FirstOrDefault((x) => x.name == src.name) is ValueResult cmp)
                {
                    if (cmp == null) continue;

                    if (!CompareValueResult(src, cmp))
                    {
                        results.ValueResults.Add(cmp);
                        results.IsNotEmpty = true;
                    }
                }
                else
                {
                    results.ValueResults.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (Value src in Values)
                if (compare.Values.FirstOrDefault((x) => x.name == src.name) is Value cmp)
                {
                    if (cmp == null) continue;

                    if (!CompareValue(src, cmp))
                    {
                        results.Values.Add(cmp);
                        results.IsNotEmpty = true;
                    }
                }
                else
                {
                    results.Values.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (ValueResult src in Gs1ValueResults)
                if (compare.Gs1ValueResults.FirstOrDefault((x) => x.name == src.name) is ValueResult cmp)
                {
                    if (cmp == null) continue;

                    if (!CompareValueResult(src, cmp))
                    {
                        results.Gs1ValueResults.Add(cmp);
                        results.IsNotEmpty = true;
                    }
                }
                else
                {
                    results.Gs1ValueResults.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (Grade src in Gs1Grades)
                if (compare.Gs1Grades.FirstOrDefault((x) => x.name == src.name) is Grade cmp)
                {
                    if (cmp == null) continue;

                    if (!CompareGrade(src, cmp))
                    {
                        results.Gs1Grades.Add(cmp);
                        results.IsNotEmpty = true;
                    }
                }
                else
                {
                    results.Gs1Grades.Add(src);
                    results.IsNotEmpty = true;
                }

            foreach (var aS in Alarms)
            {
                bool found = false;
                foreach (var aC in compare.Alarms)
                {
                    //if (Type != "blemish")
                    //{
                        if (aS.name == aC.name)
                        {
                            found = true;
                            if (!CompareAlarm(aS, aC))
                            {
                                results.Alarms.Add(aC);
                                results.IsNotEmpty = true;
                            }
                        }
                    //}
                }

                if (!found)
                {
                    results.Alarms.Add(aS);
                    results.IsNotEmpty = true;
                }
            }

            foreach (var aS in compare.Alarms)
            {
                bool found = false;
                foreach (var aC in Alarms)
                {
                    //if (Type != "blemish")
                    //{
                    if (aS.name == aC.name)
                    {
                        found = true;
                        if (!CompareAlarm(aS, aC))
                        {
                            results.Alarms.Add(aC);
                            results.IsNotEmpty = true;
                        }
                    }
                    //}
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
