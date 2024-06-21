using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using V275_REST_lib.Models;

namespace LabelVal.Sectors.ViewModels;

public partial class SectorDifferences : ObservableObject
{

    public class GradeValue : Report_InspectSector_Common.GradeValue
    {
        public string Name { get; set; }

        public GradeValue(string name, Report_InspectSector_Common.GradeValue data)
        {
            if(data != null)
            {
                value = data.value;
                grade = data.grade;
            }
            Name = name;
        }
    }
    public class Grade : Report_InspectSector_Common.Grade
    {
        public string Name { get; set; }

        public Grade(string name, Report_InspectSector_Common.Grade data)
        {
            value = data.value;
            letter = data.letter;
            Name = name;
        }
    }
    public class ValueResult : Report_InspectSector_Common.ValueResult
    {
        public string Name { get; set; }

        public ValueResult(string name, Report_InspectSector_Common.ValueResult data)
        {
            value = data.value;
            result = data.result;
            Name = name;
        }
    }
    public class Value : Report_InspectSector_Common.Value
    {
        public string Name { get; set; }

        public Value(string name, Report_InspectSector_Common.Value data)
        {
            value = data.value;
            Name = name;
        }
    }
    public class Blemish : Report_InspectSector_Blemish.Blemish
    {
        public System.Drawing.Rectangle Rectangle => new(top, left, width, height);

        public Blemish(Report_InspectSector_Blemish.Blemish data)
        {
            top = data.top;
            left = data.left;
            height = data.height;
            width = data.width;
            type = data.type;
        }
    }

    private SectorDifferencesSettings Settings { get; } = new SectorDifferencesSettings();

    [ObservableProperty] private string userName;
    [ObservableProperty] private string type;
    [ObservableProperty] private string units;
    [ObservableProperty] private bool isNotOCVMatch = false;
    [ObservableProperty] private string oCVMatchText;
    [ObservableProperty] private bool isSectorMissing;
    [ObservableProperty] private string sectorMissingText;
    [ObservableProperty] private bool isNotEmpty = false;
    //[ObservableProperty] private bool isGS1Standard;

    public ObservableCollection<GradeValue> GradeValues { get; } = [];
    public ObservableCollection<ValueResult> ValueResults { get; } = [];
    public ObservableCollection<ValueResult> Gs1ValueResults { get; } = [];
    public ObservableCollection<Grade> Gs1Grades { get; } = [];
    public ObservableCollection<Value> Values { get; } = [];
    public ObservableCollection<Report_InspectSector_Common.Alarm> Alarms { get; } = [];
    public ObservableCollection<Blemish> Blemishes { get; } = [];

    public void V275Process(object verify, string userName)
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

    public void V5Process(V5_REST_Lib.Models.Results_QualifiedResult results, string userName)
    {
        UserName = userName;
        IsNotEmpty = false;

        Type = V5GetSymbolType(results);
        Units = "mil";

        OCVMatchText = null;
        Blemishes.Clear();

        GradeValues.Clear();
        ValueResults.Clear();
        Values.Clear();
        Alarms.Clear();
        Gs1ValueResults.Clear();
        Gs1Grades.Clear();

        if (Type == "verify2D" && results.grading.iso15415 != null)
        {
            IsNotEmpty = true;

            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.decode.letter), value = results.grading.iso15415.decode.grade },
                    value = results.grading.iso15415.decode.value
                }));

            GradeValues.Add(new GradeValue("contrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.contrast.letter), value = results.grading.iso15415.contrast.grade },
                    value = results.grading.iso15415.contrast.value
                }));

            GradeValues.Add(new GradeValue("modulation",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.modulation.letter), value = results.grading.iso15415.modulation.grade },
                    value = results.grading.iso15415.modulation.value
                }));

            GradeValues.Add(new GradeValue("reflectanceMargin",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.reflectanceMargin.letter), value = results.grading.iso15415.reflectanceMargin.grade },
                    value = results.grading.iso15415.reflectanceMargin.value
                }));

            GradeValues.Add(new GradeValue("axialNonUniformity",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.axialNonUniformity.letter), value = results.grading.iso15415.axialNonUniformity.grade },
                    value = results.grading.iso15415.axialNonUniformity.value
                }));

            GradeValues.Add(new GradeValue("gridNonUniformity",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.gridNonUniformity.letter), value = results.grading.iso15415.gridNonUniformity.grade },
                    value = results.grading.iso15415.gridNonUniformity.value
                }));

            GradeValues.Add(new GradeValue("unusedECC",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.unusedECC.letter), value = results.grading.iso15415.unusedECC.grade },
                    value = results.grading.iso15415.unusedECC.value
                }));

            GradeValues.Add(new GradeValue("fixedPatternDamage",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15415.fixedPatternDamage.letter), value = results.grading.iso15415.fixedPatternDamage.grade },
                    value = results.grading.iso15415.fixedPatternDamage.value
                }));
        }
        else if (Type == "verify1D" && results.grading.iso15416 is { overall: not null })
        {
            IsNotEmpty = true;

            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.decode.letter), value = results.grading.iso15416.decode.grade },
                    value = results.grading.iso15416.decode.value
                }));

            GradeValues.Add(new GradeValue("symbolContrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.symbolContrast.letter), value = results.grading.iso15416.symbolContrast.grade },
                    value = results.grading.iso15416.symbolContrast.value
                }));

            GradeValues.Add(new GradeValue("minimumEdgeContrast",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.minimumEdgeContrast.letter), value = results.grading.iso15416.minimumEdgeContrast.grade },
                    value = results.grading.iso15416.minimumEdgeContrast.value
                }));

            GradeValues.Add(new GradeValue("modulation",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.modulation.letter), value = results.grading.iso15416.modulation.grade },
                    value = results.grading.iso15416.modulation.value
                }));

            GradeValues.Add(new GradeValue("defects",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.defects.letter), value = results.grading.iso15416.defects.grade },
                    value = results.grading.iso15416.defects.value
                }));

            GradeValues.Add(new GradeValue("decodability",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.decodability.letter), value = results.grading.iso15416.decodability.grade },
                    value = results.grading.iso15416.decodability.value
                }));

            GradeValues.Add(new GradeValue("minimumReflectance",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.minimumReflectance.letter), value = results.grading.iso15416.minimumReflectance.grade },
                    value = results.grading.iso15416.minimumReflectance.value
                }));



            //GradeValues.Add(new GradeValue("edgeDetermination",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.edgeDetermination.letter), value = results.grading.iso15416.edgeDetermination.grade },
            //        value = results.grading.iso15416.edgeDetermination.value
            //    }));

            //GradeValues.Add(new GradeValue("quietZone",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.quietZone.letter), value = results.grading.iso15416.quietZone.grade },
            //        value = results.grading.iso15416.quietZone.value
            //    }));

            ValueResults.Add(new ValueResult("edgeDetermination", new Report_InspectSector_Common.ValueResult() { value = results.grading.iso15416.edgeDetermination.value, result = results.grading.iso15416.edgeDetermination.letter == 65 ? "PASS" : "FAIL" }));

            ValueResults.Add(new ValueResult("quietZone", new Report_InspectSector_Common.ValueResult() { value = results.grading.iso15416.quietZone.value, result = results.grading.iso15416.quietZone.letter == 65 ? "PASS" : "FAIL" }));


            //GradeValues.Add(new GradeValue("overall",
            //    new Report_InspectSector_Common.GradeValue()
            //    {
            //        grade = new Report_InspectSector_Common.Grade() { letter = V5GetGradeLetter(results.grading.iso15416.overall.letter), value = results.grading.iso15416.overall.grade },
            //        value = results.grading.iso15416.overall.value
            //    }));




        }

        if (Type == "verify2D")
        {
            if (results.Datamatrix != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.Datamatrix.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.Datamatrix.columns }));


                Values.Add(new Value("uec", new Report_InspectSector_Common.Value() { value = results.Datamatrix.uec }));
                Values.Add(new Value("ecc", new Report_InspectSector_Common.Value() { value = results.Datamatrix.ecc }));

                Values.Add(new Value("mirror", new Report_InspectSector_Common.Value() { value = results.Datamatrix.mirror ? 1 : 0 }));
                Values.Add(new Value("readerConfig", new Report_InspectSector_Common.Value() { value = results.Datamatrix.readerConfig ? 1 : 0 }));
            }
            else if (results.QR != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.QR.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.QR.columns }));


                Values.Add(new Value("uec", new Report_InspectSector_Common.Value() { value = results.QR.uec }));
                //Values.Add(new Value("ecl", new Report_InspectSector_Common.Value() { value = results.QR.ecl }));

                Values.Add(new Value("mirror", new Report_InspectSector_Common.Value() { value = results.QR.mirror ? 1 : 0 }));
                Values.Add(new Value("model", new Report_InspectSector_Common.Value() { value = results.QR.model }));
                Values.Add(new Value("locatorCount", new Report_InspectSector_Common.Value() { value = results.QR.locator.Count() }));
            }


        }
        else if (Type == "verify1D")
        {
            if (results.Code128 != null)
                Values.Add(new Value("barCount", new Report_InspectSector_Common.Value() { value = results.Code128.barCount }));
            else if (results.PDF417 != null)
            {
                Values.Add(new Value("rows", new Report_InspectSector_Common.Value() { value = results.PDF417.rows }));
                Values.Add(new Value("columns", new Report_InspectSector_Common.Value() { value = results.PDF417.columns }));

                Values.Add(new Value("ecc", new Report_InspectSector_Common.Value() { value = results.PDF417.ecc }));
            }
            else if (results.UPC != null)
            {
                Values.Add(new Value("barCount", new Report_InspectSector_Common.Value() { value = results.UPC.barCount }));
                Values.Add(new Value("supplemental", new Report_InspectSector_Common.Value() { value = results.UPC.supplemental }));

                //Values.Add(new Value("version", new Report_InspectSector_Common.Value() { value = results.UPC.version }));
            }
        }

    }

    #region L95xx
    public void L95xxProcess(List<string> splitPacket, string userName, bool isPDF417)
    {
        UserName = userName;
        IsNotEmpty = false;

        float cellSizeX = 0, cellSizeY = 0;
        var alarms = new List<Report_InspectSector_Common.Alarm>();

        if (splitPacket.Find((e) => e.StartsWith("Cell size")) != null)
        {
            //Verify 2D

            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                IsNotEmpty = true;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Warning"))
                {
                    alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                    continue;
                }

                if (spl1[0].Equals("Decode"))
                {
                    GradeValues.Add(new GradeValue("decode",
                        new Report_InspectSector_Common.GradeValue()
                        {
                            grade = spl1[1].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                            value = -1
                        }));
                    continue;
                }

                if (spl1[0].Equals("Contrast"))
                {
                    GradeValues.Add(new GradeValue("contrast", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Modulation"))
                {
                    GradeValues.Add(new GradeValue("modulation", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }

                if (spl1[0].StartsWith("Reflectance"))
                {
                    GradeValues.Add(new GradeValue("reflectance", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }

                if (spl1[0].StartsWith("Axial "))
                {
                    GradeValues.Add(new GradeValue("axialNonUniformity", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Grid "))
                {
                    GradeValues.Add(new GradeValue("gridNonUniformity", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(new GradeValue("unusedECC", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Fixed"))
                {
                    GradeValues.Add(new GradeValue("fixedPatternDamage", GetGradeValue(spl1[1])));
                    continue;
                }


                if (spl1[0].StartsWith("Rmin"))
                {
                    Values.Add(new Value("minimumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) }));
                    continue;
                }
                if (spl1[0].StartsWith("Rmax"))
                {
                    Values.Add(new Value("maximumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) }));
                    continue;
                }


                if (spl1[0].StartsWith("X print"))
                {
                    Gs1Grades.Add(new Grade("growthX", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("Y print"))
                {
                    Gs1Grades.Add(new Grade("growthY", GetGrade(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Cell height"))
                {
                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    cellSizeX = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellHeight", new Report_InspectSector_Common.ValueResult() { value = cellSizeX, result = item == null ? "PASS" : "FAIL" }));
                    continue;
                }
                if (spl1[0].StartsWith("Cell width"))
                {
                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    cellSizeY = ParseFloat(spl1[1]);

                    ValueResults.Add(new ValueResult("cellWidth", new Report_InspectSector_Common.ValueResult() { value = cellSizeY, result = item == null ? "PASS" : "FAIL" }));

                    continue;
                }
                if (spl1[0].Equals("Size"))
                {
                    var spl2 = spl1[1].Split('x');

                    ValueResults.Add(new ValueResult("symbolWidth", new Report_InspectSector_Common.ValueResult() { value = cellSizeX * ParseInt(spl2[0]), result = "PASS" }));
                    ValueResults.Add(new ValueResult("symbolHeight", new Report_InspectSector_Common.ValueResult() { value = cellSizeY * ParseInt(spl2[1]), result = "PASS" }));
                    continue;
                }

                if (spl1[0].StartsWith("L1 ("))
                {
                    Gs1Grades.Add(new Grade("L1", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("L2"))
                {
                    Gs1Grades.Add(new Grade("L2", GetGrade(spl1[1])));
                    //sect.data.gs1SymbolQuality.L2 = GetGrade(spl1[1]);
                    continue;
                }
                if (spl1[0].StartsWith("QZL1"))
                {
                    Gs1Grades.Add(new Grade("QZL1", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("QZL2"))
                {

                    Gs1Grades.Add(new Grade("QZL2", GetGrade(spl1[1])));
                    continue;
                }
                if (spl1[0].StartsWith("OCTASA"))
                {
                    Gs1Grades.Add(new Grade("OCTASA", GetGrade(spl1[1])));
                    continue;
                }
            }

            foreach (var item in alarms)
                Alarms.Add(item);
        }
        else
        {
            GradeValues.Add(new GradeValue("decode",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = GetValues("Decode,", splitPacket)[0].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                    value = -1
                }));

            GradeValues.Add(new GradeValue("symbolContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            // GradeValues.Add(new GradeValue("edgeContrast", GetGradeValue(GetValues("Contrast", splitPacket)[0])));
            GradeValues.Add(new GradeValue("modulation", GetGradeValue(GetValues("Modulation", splitPacket)[0])));
            GradeValues.Add(new GradeValue("defects", GetGradeValue(GetValues("Defects", splitPacket)[0])));
            GradeValues.Add(new GradeValue("decodability", GetGradeValue(GetValues("Decodability", splitPacket)[0])));
            GradeValues.Add(new GradeValue("MinRef",
                new Report_InspectSector_Common.GradeValue()
                {
                    grade = GetValues("Min Ref", splitPacket)[0].StartsWith("PASS") ? new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } : new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f },
                    value = -1
                }));

            if (!isPDF417)
                Values.Add(new Value("maximumReflectance", new Report_InspectSector_Common.Value() { value = ParseInt(GetValues("Rmax", splitPacket)[0]) }));

            ValueResults.Add(new ValueResult("edgeDetermination", new Report_InspectSector_Common.ValueResult() { value = 100, result = GetValues("Edge", splitPacket)[0] }));

            foreach (var data in splitPacket)
            {
                if (!data.Contains(','))
                    continue;

                var spl1 = new string[2];
                spl1[0] = data.Substring(0, data.IndexOf(','));
                spl1[1] = data.Substring(data.IndexOf(',') + 1);

                if (spl1[0].StartsWith("Warning"))
                {
                    alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                    continue;
                }

                if (spl1[0].StartsWith("Rmin"))
                {
                    if (isPDF417) continue;

                    var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                    Values.Add(new Value("minimumReflectance", new Report_InspectSector_Common.Value() { value = val }));
                    continue;
                }


                if (spl1[0].StartsWith("Unused "))
                {
                    GradeValues.Add(new GradeValue("unusedErrorCorrection", GetGradeValue(spl1[1])));
                    continue;
                }

                if (spl1[0].StartsWith("Xdim"))
                {
                    var xdim = ParseFloat(spl1[1]);

                    if (isPDF417) continue;

                    var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                    ValueResults.Add(new ValueResult("symbolXDim", new Report_InspectSector_Common.ValueResult() { value = xdim, result = item == null ? "PASS" : "FAIL" }));

                    continue;
                }

                if (spl1[0].StartsWith("Bar height"))
                {
                    var val = ParseFloat(spl1[1]) * 1000;

                    var item = alarms.Find((e) => e.name.Contains("minimum height"));

                    ValueResults.Add(new ValueResult("barHeight", new Report_InspectSector_Common.ValueResult() { value = val, result = item == null ? "PASS" : "FAIL" }));
                    continue;
                }

                if (spl1[0].StartsWith("Quiet"))
                {
                    if (spl1[1].Contains("ERR"))
                    {
                        var spl2 = spl1[1].Split(' ');

                        if (spl2.Count() != 2) continue;


                        ValueResults.Add(new ValueResult("quietZoneLeft", new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] }));
                        ValueResults.Add(new ValueResult("quietZoneRight", new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] }));
                    }
                    else
                    {
                        ValueResults.Add(new ValueResult("quietZoneLeft", new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] }));
                        ValueResults.Add(new ValueResult("quietZoneRight", new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] }));
                    }

                    continue;
                }

                if (spl1[0].StartsWith("Codeword y"))
                {
                    var spl2 = spl1[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (spl2.Count() != 2) continue;

                    GradeValues.Add(new GradeValue("CodewordY", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl2[0]), value = ParseInt(spl2[1]) }));
                    continue;
                }

                if (spl1[0].StartsWith("Codeword P"))
                {
                    GradeValues.Add(new GradeValue("CodewordP", new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 }));
                    continue;
                }
            }

            foreach (var item in alarms)
                Alarms.Add(item);

        }
    }
    private string[] GetValues(string name, List<string> splitPacket)
    {
        var warn = splitPacket.FindAll((e) => e.StartsWith(name));

        var ret = new List<string>();
        foreach (var line in warn)
        {
            //string[] spl1 = new string[2];
            //spl1[0] = line.Substring(0, line.IndexOf(','));
            ret.Add(line.Substring(line.IndexOf(',') + 1));
        }
        return ret.ToArray();
    }
    private float ParseFloat(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789.").Contains(c)
                                ).ToArray());

        if (float.TryParse(digits, out var val))
            return val;
        else
            return 0;

    }

    private static int ParseInt(string value)
    {
        var digits = new string(value.Trim().TakeWhile(c =>
                                ("0123456789").Contains(c)
                                ).ToArray());

        return int.TryParse(digits, out var val) ? val : 0;
    }

    private Report_InspectSector_Common.GradeValue GetGradeValue(string data)
    {
        var spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (spl2.Count() != 2)
            return null;

        var tmp = ParseFloat(spl2[0]);

        return new Report_InspectSector_Common.GradeValue()
        {
            grade = new Report_InspectSector_Common.Grade()
            {
                value = tmp,
                letter = GetLetter(tmp)
            },
            value = ParseInt(spl2[1])
        };

    }

    private Report_InspectSector_Common.Grade GetGrade(string data)
    {
        var tmp = ParseFloat(data);

        return new Report_InspectSector_Common.Grade()
        {
            value = tmp,
            letter = GetLetter(tmp)
        };
    }

    private static string GetLetter(float value)
    {
        return value switch
        {
            4.0f => "A",
            <= 3.9f and >= 3.0f => "B",
            <= 2.9f and >= 2.0f => "C",
            <= 1.9f and >= 1.0f => "D",
            <= 0.9f and >= 0.0f => "F",
            _ => "F"
        };
    }

    #endregion

    private static string V5GetGradeLetter(int grade) => grade switch
    {
        65 => "A",
        66 => "B",
        67 => "C",
        68 => "D",
        70 => "F",
        _ => throw new System.NotImplementedException(),
    };
    private static string V5GetSymbolType(V5_REST_Lib.Models.Results_QualifiedResult results)
    {
        if (results.Code128 != null)
            return "verify1D";
        else if (results.Datamatrix != null)
            return "verify2D";
        else if (results.QR != null)
            return "verify2D";
        else if (results.PDF417 != null)
            return "verify1D";
        else return results.UPC != null ? "verify1D" : "Unknown";
    }

    private string FormatName(string name)
    {
        var tmp = string.Concat(name.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
        return $"{char.ToUpper(tmp[0])}{tmp[1..]}";
    }

    public SectorDifferences Compare(SectorDifferences compare)
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

        //foreach (Blemish src in Blemishes)
        //    if (compare.Blemishes.FirstOrDefault((x) => x.rectangle.Contains(new System.Drawing.Point(src.rectangle.Left + (src.width / 2), src.rectangle.Top + (src.rectangle.Height / 2)))) is Blemish cmp)
        //    {
        //        //if (cmp == null)
        //        //{
        //        //    results.Blemishes.Add(src);
        //        //    results.IsNotEmpty = true;
        //        //    continue;
        //        //}

        //        //results.Blemishes.Add(cmp);
        //        //results.IsNotEmpty = true;

        //    }
        //    else
        //    {
        //        results.Blemishes.Add(src);
        //        results.IsNotEmpty = true;
        //    }

        foreach (var src in GradeValues)
            if (compare.GradeValues.FirstOrDefault((x) => x.Name == src.Name) is GradeValue cmp)
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

        foreach (var src in ValueResults)
            if (compare.ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
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

        foreach (var src in Values)
            if (compare.Values.FirstOrDefault((x) => x.Name == src.Name) is Value cmp)
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

        foreach (var src in Gs1ValueResults)
            if (compare.Gs1ValueResults.FirstOrDefault((x) => x.Name == src.Name) is ValueResult cmp)
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

        foreach (var src in Gs1Grades)
            if (compare.Gs1Grades.FirstOrDefault((x) => x.Name == src.Name) is Grade cmp)
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
            var found = false;
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
            var found = false;
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

    private bool CompareGrade(Report_InspectSector_Common.Grade source, Report_InspectSector_Common.Grade compare)
    {
        return Settings.Grade_UseGradeLetter
            ? source.letter == compare.letter
            : (compare.value <= source.value + Settings.Grade_GradeValueTolerance) && (compare.value >= source.value - Settings.Grade_GradeValueTolerance);
    }
    private bool CompareGradeValue(Report_InspectSector_Common.GradeValue source, Report_InspectSector_Common.GradeValue compare)
    {
        return Settings.GradeValue_UseGradeLetter
            ? source.grade.letter == compare.grade.letter
            : Settings.GradeValue_UseValue
                ? (compare.value <= source.value + Settings.GradeValue_ValueTolerance) && (compare.value >= source.value - Settings.GradeValue_ValueTolerance)
                : (compare.grade.value <= source.grade.value + Settings.GradeValue_GradeValueTolerance) && (compare.grade.value >= source.grade.value - Settings.GradeValue_GradeValueTolerance);
    }
    private bool CompareValueResult(Report_InspectSector_Common.ValueResult source, Report_InspectSector_Common.ValueResult compare)
    {
        return Settings.ValueResult_UseResult
            ? source.result == compare.result
            : (compare.value <= source.value + Settings.ValueResult_ValueTolerance) && (compare.value >= source.value - Settings.ValueResult_ValueTolerance);
    }
    private bool CompareValue(Report_InspectSector_Common.Value source, Report_InspectSector_Common.Value compare) => (compare.value <= source.value + Settings.Value_ValueTolerance) && (compare.value >= source.value - Settings.Value_ValueTolerance);
    private static bool CompareAlarm(Report_InspectSector_Common.Alarm source, Report_InspectSector_Common.Alarm compare) => source.category == compare.category && source.data.subAlarm == compare.data.subAlarm;

    //public void Clear()
    //{
    //    GradeValues.Clear();
    //    ValueResults.Clear();
    //    Gs1ValueResults.Clear();
    //    Gs1Grades.Clear();
    //    Values.Clear();
    //    Alarms.Clear();
    //    Blemishes.Clear();

    //}
}
