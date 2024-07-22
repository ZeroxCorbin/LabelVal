using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.V275.Sectors;

public partial class SectorDifferences : ObservableObject, ISectorDifferences
{
    [ObservableProperty] private string userName;
    [ObservableProperty] private string type;
    [ObservableProperty] private string units;
    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;
    [ObservableProperty] private bool isNotEmpty = false;

    public ObservableCollection<GradeValue> GradeValues { get; } = [];
    public ObservableCollection<ValueResult> ValueResults { get; } = [];
    public ObservableCollection<ValueResult> Gs1ValueResults { get; } = [];
    public ObservableCollection<Grade> Gs1Grades { get; } = [];
    public ObservableCollection<Value> Values { get; } = [];
    public ObservableCollection<Report_InspectSector_Common.Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDifferences() { }
    public SectorDifferences(object verify, string userName) => Process(verify, userName);
    public ISectorDifferences Compare(ISectorDifferences compare)
    {
        var results = new SectorDifferences
        {
            UserName = UserName,
            Type = Type
        };

        if (Type is "ocr" or "ocr")
        {
            if (!OCVMatchText.Equals(compare.OCVMatchText))
            {
                if (compare.OCVMatchText != null)
                {
                    results.IsNotEmpty = true;
                    results.IsNotOCVMatch = true;
                    results.OCVMatchText = $"{OCVMatchText} / {compare.OCVMatchText}";
                }
            }
        }

        foreach (var src in GradeValues)
            if (compare.GradeValues.FirstOrDefault((x) => x.Name == src.Name) is GradeValue cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareGradeValue(src, cmp))
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

        foreach (var src in ValueResults)
            if (compare.ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValueResult(src, cmp))
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

        foreach (var src in Values)
            if (compare.Values.FirstOrDefault((x) => x.Name == src.Name) is Value cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValue(src, cmp))
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

        foreach (var src in Gs1ValueResults)
            if (compare.Gs1ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareValueResult(src, cmp))
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

        foreach (var src in Gs1Grades)
            if (compare.Gs1Grades.FirstOrDefault((x) => x.Name == src.Name) is Grade cmp)
            {
                if (cmp == null) continue;

                if (!ISectorDifferences.CompareGrade(src, cmp))
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
            var found = false;
            foreach (var aC in compare.Alarms)
            {
                //if (Type != "blemish")
                //{
                if (aS.name == aC.name)
                {
                    found = true;
                    if (!ISectorDifferences.CompareAlarm(aS, aC))
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
            var found = false;
            foreach (var aC in Alarms)
            {
                //if (Type != "blemish")
                //{
                if (aS.name == aC.name)
                {
                    found = true;
                    if (!ISectorDifferences.CompareAlarm(aS, aC))
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
                    if (prop1.Name == "lengthUnit")
                        Units = (string)prop1.GetValue(prop.GetValue(verify));

                    if (Type is "ocr" or "ocv")
                    {
                        if (prop1.Name == "text")
                            OCVMatchText = (string)prop1.GetValue(prop.GetValue(verify));

                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Blemish.Blemish[]))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Blemish.Blemish[] dat)
                        {
                            foreach (var d in dat)
                                Blemishes.Add(new Blemish(d));

                            IsNotEmpty = Blemishes.Count > 0;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Decode))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Decode dat)
                        {
                            GradeValues.Add(new GradeValue(prop1.Name, new Report_InspectSector_Common.GradeValue() { grade = dat.grade, value = dat.value }));

                            if (dat.edgeDetermination != null)
                                if (Type == "verify1D")
                                    ValueResults.Add(new ValueResult("edgeDetermination", dat.edgeDetermination));

                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.GradeValue))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.GradeValue dat)
                        {
                            GradeValues.Add(new GradeValue(prop1.Name, dat));
                            IsNotEmpty = true;

                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.ValueResult))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.ValueResult dat)
                        {
                            ValueResults.Add(new ValueResult(prop1.Name, dat));
                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Value))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Value dat)
                        {
                            Values.Add(new Value(prop1.Name, dat));
                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Alarm[]))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Alarm[] dat)
                        {
                            foreach (var d in dat)
                                Alarms.Add(d);

                            IsNotEmpty = Alarms.Count > 0;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Verify1D.Gs1symbolquality) || prop1.PropertyType == typeof(Report_InspectSector_Verify2D.Gs1symbolquality))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) != null)
                            foreach (var prop2 in prop1.GetValue(prop.GetValue(verify)).GetType().GetProperties())
                            {
                                if (prop2.PropertyType == typeof(Report_InspectSector_Common.ValueResult))
                                {
                                    if (prop2.GetValue(prop1.GetValue(prop.GetValue(verify))) is Report_InspectSector_Common.ValueResult dat)
                                    {
                                        Gs1ValueResults.Add(new ValueResult(prop2.Name, dat));
                                        IsNotEmpty = true;
                                    }
                                    continue;
                                }
                                if (prop2.PropertyType == typeof(Report_InspectSector_Common.Grade))
                                {
                                    if (prop2.GetValue(prop1.GetValue(prop.GetValue(verify))) is Report_InspectSector_Common.Grade dat)
                                    {
                                        Gs1Grades.Add(new Grade(prop2.Name, dat));
                                        IsNotEmpty = true;
                                    }
                                    continue;
                                }
                            }
                        //ValueResults.Add(prop1.Name, (Report_InspectSector_Common.ValueResult)prop1.GetValue(prop.GetValue(verify)));
                        continue;
                    }
                }
        }
    }
}
