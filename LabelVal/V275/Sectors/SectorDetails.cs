using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_Lib.Models;

namespace LabelVal.V275.Sectors;

public partial class SectorDetails : ObservableObject, ISectorDetails
{
    [ObservableProperty] private string name;
    [ObservableProperty] private string userName;
    [ObservableProperty] private string symbolType;
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
    public ObservableCollection<Value_> Values { get; } = [];
    public ObservableCollection<Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public SectorDetails() { }
    public SectorDetails(object verify, string userName) => Process(verify, userName);
    public SectorDifferences Compare(ISectorDetails compare) => SectorDifferences.Compare(this, compare);

    public void Process(object verify, string userName)
    {
        UserName = userName;
        IsNotEmpty = false;

        foreach (var prop in verify.GetType().GetProperties())
        {
            if (prop.Name == "type")
                SymbolType = prop.GetValue(verify).ToString();

            if (prop.Name == "data")
                foreach (var prop1 in prop.GetValue(verify).GetType().GetProperties())
                {
                    if (prop1.Name == "lengthUnit")
                        Units = (string)prop1.GetValue(prop.GetValue(verify));

                    if (SymbolType is "ocr" or "ocv")
                    {
                        if (prop1.Name == "text")
                            OCVMatchText = (string)prop1.GetValue(prop.GetValue(verify));

                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Blemish.Blemish[]))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Blemish.Blemish[] dat)
                        {
                            foreach (var d in dat)
                                Blemishes.Add(new Blemish(d.top, d.left, d.height, d.width, d.type));

                            IsNotEmpty = Blemishes.Count > 0;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Decode))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Decode dat)
                        {
                            GradeValues.Add(new GradeValue(prop1.Name, dat.value, new Grade(prop1.Name, dat.grade.value, dat.grade.letter)));

                            if (dat.edgeDetermination != null)
                                if (SymbolType == "verify1D")
                                    ValueResults.Add(new ValueResult("edgeDetermination", dat.edgeDetermination.value, dat.edgeDetermination.result));

                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.GradeValue))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.GradeValue dat)
                        {
                            GradeValues.Add(new GradeValue(prop1.Name, dat.value, new Grade(prop1.Name, dat.grade.value, dat.grade.letter)));
                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.ValueResult))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.ValueResult dat)
                        {
                            ValueResults.Add(new ValueResult(prop1.Name, dat.value, dat.result));
                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Value))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Value dat)
                        {
                            Values.Add(new Value_(prop1.Name, dat.value));
                            IsNotEmpty = true;
                        }
                        continue;
                    }

                    if (prop1.PropertyType == typeof(Report_InspectSector_Common.Alarm[]))
                    {
                        if (prop1.GetValue(prop.GetValue(verify)) is Report_InspectSector_Common.Alarm[] dat)
                        {
                            foreach (var d in dat)
                            {
                                Alarms.Add(new Alarm()
                                {
                                    Category = d.category,
                                    Name = d.name,
                                    Data = new SubAlarm_()
                                    {
                                        Index = d.data.index,
                                        Expected = d.data.expected,
                                        SubAlarm = d.data.subAlarm,
                                        Text = d.data.text
                                    },
                                    UserAction = new Useraction()
                                    {
                                        Action = d.userAction?.action,
                                        Note = d.userAction?.note,
                                        User = d.userAction?.user
                                    }
                                });
                            }

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
                                        Gs1ValueResults.Add(new ValueResult(prop2.Name, dat.value, dat.result));
                                        IsNotEmpty = true;
                                    }
                                    continue;
                                }
                                if (prop2.PropertyType == typeof(Report_InspectSector_Common.Grade))
                                {
                                    if (prop2.GetValue(prop1.GetValue(prop.GetValue(verify))) is Report_InspectSector_Common.Grade dat)
                                    {
                                        Gs1Grades.Add(new Grade(prop2.Name, dat.value, dat.letter));
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
