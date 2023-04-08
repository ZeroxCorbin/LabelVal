using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using V275_REST_lib.Models;

namespace LabelVal.LVS_95xx
{
    internal class LVS95xx_SerialPort
    {
        public delegate void PacketAvailableDelegate(string packet);
        public event PacketAvailableDelegate PacketAvailable;

        public event EventHandler Exception;

        public delegate void SectorAvailableDelegate(object sector);
        public event SectorAvailableDelegate SectorAvailable;

        public SerialPortController Controller { get; } = new SerialPortController();

        private bool running;
        private bool newData;
        private bool listening;

        private string readData;

        private object dataLock = new object();


        public int Init(string portName)
        {
            Controller.GetCOMList();
            var com = Controller.COMPortsAvailable.Find((e) => e.Equals(portName));

            if (com == null)
                return -1;

            Controller.Init(portName);

            return 0;
        }

        public bool Start()
        {
            ReleaseEvents();

            PacketAvailable += LVS95xx_SerialPort_PacketAvailable;
            Controller.Exception += Controller_Exception;
            Controller.DataAvailable += Controller_DataAvailable;

            if (Controller.Connect())
            {
                Task.Run(() => PacketThread());
                return true;
            }

            ReleaseEvents();

            return false;

        }

        private void ReleaseEvents()
        {
            PacketAvailable -= LVS95xx_SerialPort_PacketAvailable;
            Controller.Exception -= Controller_Exception;
            Controller.DataAvailable -= Controller_DataAvailable;
        }

        private void Controller_Exception(object sender, EventArgs e)
        {
            Stop();

            Exception?.Invoke(sender, e);
        }

        private void LVS95xx_SerialPort_PacketAvailable(string packet)
        {
            var spl = packet.Split('\r').ToList();

            var elm = spl.Find((e) => e.StartsWith("Cell size"));

            if (elm != null)
            {
                //Verify 2D
                Report_InspectSector_Verify2D sect = new Report_InspectSector_Verify2D();
                sect.data = new Report_InspectSector_Verify2D.Data();
                sect.data.gs1SymbolQuality = new Report_InspectSector_Verify2D.Gs1symbolquality();
                sect.type = "verify2D";

                List<Report_InspectSector_Common.Alarm> alarms = new List<Report_InspectSector_Common.Alarm>();

                foreach (var data in spl)
                {
                    if (!data.Contains(','))
                        continue;

                    string[] spl1 = new string[2];
                    spl1[0] = data.Substring(0, data.IndexOf(','));
                    spl1[1] = data.Substring(data.IndexOf(',') + 1);

                    if (spl1[0].StartsWith("Symbology"))
                    {
                        sect.data.symbolType = GetSymbolType(spl1[1]);
                        continue;
                    }

                    if (spl1[0].StartsWith("Overall"))
                    {
                        var spl2 = spl1[1].Split('/');

                        if (spl2.Count() < 3) continue;

                        sect.data.overallGrade = new Report_InspectSector_Common.Overallgrade() { grade = GetGrade(spl2[0]), _string = spl1[1] };
                        sect.data.aperture = ParseFloat(spl2[1]);
                        continue;
                    }

                    if (spl1[0].StartsWith("Warning"))
                    {
                        alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                        continue;
                    }

                    if (spl1[0].StartsWith("Cell size"))
                    {
                        sect.data.xDimension = ParseFloat(spl1[1]);
                        continue;
                    }


                    if (spl1[0].StartsWith("Decoded"))
                    {
                        sect.data.decodeText = spl1[1];
                        continue;
                    }

                    if (spl1[0].Equals("Decode"))
                    {
                        if (sect.data.decode == null)
                            sect.data.decode = new Report_InspectSector_Common.Decode();

                        sect.data.decode.value = -1;
                        sect.data.decode.grade = spl1[1].StartsWith("PASS") ?
                            new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } :
                            new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f };
                        continue;
                    }

                    if (spl1[0].StartsWith("Rmin"))
                    {
                        sect.data.minimumReflectance = new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) };
                        continue;
                    }
                    if (spl1[0].StartsWith("Rmax"))
                    {
                        sect.data.maximumReflectance = new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) };
                        continue;
                    }

                    if (spl1[0].StartsWith("Modulation"))
                    {
                        sect.data.modulation = new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 };
                        continue;
                    }
                    if (spl1[0].StartsWith("Reflectance"))
                    {
                        sect.data.reflectanceMargin = new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 };
                        continue;
                    }
                    if (spl1[0].StartsWith("Fixed"))
                    {
                        sect.data.fixedPatternDamage = new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 };
                        continue;
                    }


                    if (spl1[0].Equals("Contrast"))
                    {
                        sect.data.symbolContrast = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Axial "))
                    {
                        sect.data.axialNonUniformity = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Grid "))
                    {
                        sect.data.gridNonUniformity = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Unused "))
                    {
                        sect.data.unusedErrorCorrection = GetGradeValue(spl1[1]);
                        continue;
                    }

                    if (spl1[0].StartsWith("X print"))
                    {
                        sect.data.gs1SymbolQuality.growthX = ParseInt(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Y print"))
                    {
                        sect.data.gs1SymbolQuality.growthY = ParseInt(spl1[1]);
                        continue;
                    }

                    if (spl1[0].StartsWith("Cell height"))
                    {
                        var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));
                        if (item != null)
                            sect.data.gs1SymbolQuality.cellSizeX = new Report_InspectSector_Common.ValueResult() { value = ParseFloat(spl1[1]), result = "FAIL" };
                        else
                            sect.data.gs1SymbolQuality.cellSizeX = new Report_InspectSector_Common.ValueResult() { value = ParseFloat(spl1[1]), result = "PASS" };
                        continue;
                    }
                    if (spl1[0].StartsWith("Cell width"))
                    {
                        var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));
                        if (item != null)
                            sect.data.gs1SymbolQuality.cellSizeY = new Report_InspectSector_Common.ValueResult() { value = ParseFloat(spl1[1]), result = "FAIL" };
                        else
                            sect.data.gs1SymbolQuality.cellSizeY = new Report_InspectSector_Common.ValueResult() { value = ParseFloat(spl1[1]), result = "PASS" };

                        continue;
                    }
                    if (spl1[0].Equals("Size"))
                    {
                        var spl2 = spl1[1].Split('x');

                        sect.data.gs1SymbolQuality.symbolWidth = new Report_InspectSector_Common.ValueResult() { value = sect.data.gs1SymbolQuality.cellSizeX.value * ParseInt(spl2[0]), result = "PASS" };
                        sect.data.gs1SymbolQuality.symbolHeight = new Report_InspectSector_Common.ValueResult() { value = sect.data.gs1SymbolQuality.cellSizeY.value * ParseInt(spl2[1]), result = "PASS" };

                        continue;
                    }

                    if (spl1[0].StartsWith("L1 ("))
                    {
                        sect.data.gs1SymbolQuality.L1 = GetGrade(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("L2"))
                    {
                        sect.data.gs1SymbolQuality.L2 = GetGrade(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("QZL1"))
                    {
                        sect.data.gs1SymbolQuality.QZL1 = GetGrade(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("QZL2"))
                    {
                        sect.data.gs1SymbolQuality.QZL2 = GetGrade(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("OCTASA"))
                    {
                        sect.data.gs1SymbolQuality.OCTASA = GetGrade(spl1[1]);
                        continue;
                    }
                }

                sect.data.alarms = alarms.ToArray();

                SectorAvailable?.Invoke(sect);
            }
            else
            {
                Report_InspectSector_Verify1D sect = new Report_InspectSector_Verify1D();
                sect.data = new Report_InspectSector_Verify1D.Data();
                sect.data.gs1SymbolQuality = new Report_InspectSector_Verify1D.Gs1symbolquality();
                sect.type = "verify1D";

                List<Report_InspectSector_Common.Alarm> alarms = new List<Report_InspectSector_Common.Alarm>();

                foreach (var data in spl)
                {
                    if (!data.Contains(','))
                        continue;

                    string[] spl1 = new string[2];
                    spl1[0] = data.Substring(0, data.IndexOf(','));
                    spl1[1] = data.Substring(data.IndexOf(',') + 1);

                    if (spl1[0].StartsWith("Symbology"))
                    {
                        sect.data.symbolType = GetSymbolType(spl1[1]);

                        if (sect.data.symbolType == "dataBar")
                        {
                            var item = spl.Find((e) => e.StartsWith("DataBar"));
                            if (item != null)
                            {
                                var spl2 = item.Split(',');

                                if (spl2.Count() != 2)
                                    continue;

                                sect.data.symbolType += spl2[1];
                            }

                        }
                        continue;
                    }

                    if (spl1[0].StartsWith("Warning"))
                    {
                        alarms.Add(new Report_InspectSector_Common.Alarm() { name = spl1[1], category = 1 });
                        continue;
                    }

                    if (spl1[0].StartsWith("Overall"))
                    {
                        var spl2 = spl1[1].Split('/');

                        if (spl2.Count() < 3) continue;

                        sect.data.overallGrade = new Report_InspectSector_Common.Overallgrade() { grade = GetGrade(spl2[0]), _string = spl1[1] };
                        sect.data.aperture = ParseFloat(spl2[1]);
                        continue;
                    }

                    if (spl1[0].Equals("Decode"))
                    {
                        if (sect.data.decode == null)
                            sect.data.decode = new Report_InspectSector_Common.Decode();

                        sect.data.decode.value = -1;
                        sect.data.decode.grade = spl1[1].StartsWith("PASS") ?
                            new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } :
                            new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f };
                        continue;
                    }
                    if (spl1[0].StartsWith("Decoded text"))
                    {
                        sect.data.decodeText = spl1[1];
                        continue;
                    }

                    if (spl1[0].StartsWith("Unused "))
                    {
                        sect.data.unusedErrorCorrection = GetGradeValue(spl1[1]);
                        continue;
                    }

                    if (spl1[0].StartsWith("Xdim"))
                    {
                        sect.data.xDimension = ParseFloat(spl1[1]);

                        if (sect.data.symbolType == "pdf417") continue;

                        var item = alarms.Find((e) => e.name.Contains("minimum Xdim"));

                        if (item != null)
                            sect.data.gs1SymbolQuality.symbolXdim = new Report_InspectSector_Common.ValueResult() { value = sect.data.xDimension, result = "FAIL" };
                        else
                            sect.data.gs1SymbolQuality.symbolXdim = new Report_InspectSector_Common.ValueResult() { value = sect.data.xDimension, result = "PASS" };

                        continue;
                    }
                    if (spl1[0].StartsWith("Bar height"))
                    {
                        var val = ParseFloat(spl1[1]) * 1000;

                        var item = alarms.Find((e) => e.name.Contains("minimum height"));
                        if (item != null)
                            sect.data.gs1SymbolQuality.symbolBarHeight = new Report_InspectSector_Common.ValueResult() { value = val, result = "FAIL" };
                        else
                            sect.data.gs1SymbolQuality.symbolBarHeight = new Report_InspectSector_Common.ValueResult() { value = val, result = "PASS" };

                        continue;
                    }

                    if (spl1[0].StartsWith("Edge"))
                    {
                        if (sect.data.decode == null)
                            sect.data.decode = new Report_InspectSector_Common.Decode();

                        sect.data.decode.edgeDetermination = new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] };
                        continue;
                    }

                    if (spl1[0].StartsWith("Quiet"))
                    {
                        if (spl1[1].Contains("ERR"))
                        {
                            var spl2 = spl1[1].Split(' ');

                            if (spl2.Count() != 2) continue;

                            sect.data.quietZoneLeft = new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] };
                            sect.data.quietZoneRight = new Report_InspectSector_Common.ValueResult() { value = ParseInt(spl2[0]), result = spl2[1] };

                        }
                        else
                        {
                            sect.data.quietZoneLeft = new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] };
                            sect.data.quietZoneRight = new Report_InspectSector_Common.ValueResult() { value = 100, result = spl1[1] };
                        }

                        continue;
                    }
                    //if (spl1[0].Equals("Decode"))
                    //{
                    //    sect.data.decode.grade = new Report_InspectSector_Common.Grade() { value = -1, result = spl1[1] };
                    //    continue;
                    //}

                    if (spl1[0].Equals("Contrast"))
                    {
                        sect.data.symbolContrast = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Modulation"))
                    {
                        sect.data.modulation = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Decodability"))
                    {
                        sect.data.decodability = GetGradeValue(spl1[1]);
                        continue;
                    }
                    if (spl1[0].StartsWith("Defects"))
                    {
                        sect.data.defects = GetGradeValue(spl1[1]);
                        continue;
                    }


                    if (spl1[0].StartsWith("Min Ref"))
                    {
                        var grd = spl1[1].StartsWith("PASS") ?
                                    new Report_InspectSector_Common.Grade() { letter = "A", value = 4.0f } :
                                    new Report_InspectSector_Common.Grade() { letter = "F", value = 0.0f };

                        if (sect.data.minimumReflectance != null)
                            sect.data.minimumReflectance.grade = grd;
                        else
                            sect.data.minimumReflectance = new Report_InspectSector_Common.GradeValue() { grade = grd };

                        continue;
                    }
                    if (spl1[0].StartsWith("Rmin"))
                    {
                        if (sect.data.symbolType == "pdf417") continue;

                        var val = (int)Math.Ceiling(ParseFloat(spl1[1]));

                        if (sect.data.minimumReflectance != null)
                            sect.data.minimumReflectance.value = val;
                        else
                            sect.data.minimumReflectance = new Report_InspectSector_Common.GradeValue() { value = val };

                        continue;
                    }
                    if (spl1[0].StartsWith("Rmax"))
                    {
                        if (sect.data.symbolType == "pdf417") continue;

                        sect.data.maximumReflectance = new Report_InspectSector_Common.Value() { value = ParseInt(spl1[1]) };
                        continue;
                    }

                    if (spl1[0].StartsWith("Codeword y"))
                    {
                        var spl2 = spl1[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (spl2.Count() != 2) continue;

                        sect.data.cwYeild = new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl2[0]), value = ParseInt(spl2[1]) };
                        continue;
                    }

                    if (spl1[0].StartsWith("Codeword P"))
                    {
                        sect.data.cwPrintQuality = new Report_InspectSector_Common.GradeValue() { grade = GetGrade(spl1[1]), value = -1 };
                        continue;
                    }
                }

                sect.data.alarms = alarms.ToArray();

                SectorAvailable?.Invoke(sect);
            }
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

        private int ParseInt(string value)
        {
            var digits = new string(value.Trim().TakeWhile(c =>
                                    ("0123456789").Contains(c)
                                    ).ToArray());

            if (int.TryParse(digits, out var val))
                return val;
            else
                return 0;

        }

        private Report_InspectSector_Common.GradeValue GetGradeValue(string data)
        {
            var spl2 = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (spl2.Count() != 2)
                return null;

            float tmp = ParseFloat(spl2[0]);

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
            float tmp = ParseFloat(data);

            return new Report_InspectSector_Common.Grade()
            {
                value = tmp,
                letter = GetLetter(tmp)
            };
        }

        private string GetLetter(float value)
        {
            if (value == 4.0f)
                return "A";

            if (value <= 3.9f && value >= 3.0f)
                return "B";

            if (value <= 2.9f && value >= 2.0f)
                return "C";

            if (value <= 1.9f && value >= 1.0f)
                return "D";

            if (value <= 0.9f && value >= 0.0f)
                return "F";

            return "F";
        }

        private string GetSymbolType(string value)
        {
            if (value.Contains("UPC-A"))
                return "upcA";

            if (value.Contains("UPC-B"))
                return "upcB";

            if (value.Contains("EAN-13"))
                return "ean13";

            if (value.Contains("EAN-8"))
                return "ean8";

            if (value.Contains("DataBar"))
                return "dataBar";

            if (value.Contains("Code 39"))
                return "code39";

            if (value.Contains("Code 93"))
                return "code93";

            if (value.StartsWith("GS1 QR"))
                return "qrCode";

            if (value.StartsWith("Micro"))
                return "microQrCode";

            if (value.Contains("Data Matrix"))
                return "dataMatrix";

            if (value.Contains("Aztec"))
                return "aztec";

            if (value.Contains("Codabar"))
                return "codaBar";

            if (value.Contains("ITF"))
                return "i2of5";

            if (value.Contains("PDF417"))
                return "pdf417";
            return "";
        }

        public async void Stop()
        {
            ReleaseEvents();

            Controller.Disconnect();

            await Task.Run(() => { listening = false; while (running) { } });
        }

        private void Controller_DataAvailable(string data)
        {
            lock (dataLock)
            {
                readData += data;
                newData = true;
            }
        }

        private void PacketThread()
        {
            listening = true;
            running = true;

            DateTime start = DateTime.MaxValue;
            while (listening)
            {
                if (newData)
                {
                    start = DateTime.Now;
                    newData = false;
                }

                if ((DateTime.Now - start) > TimeSpan.FromMilliseconds(500))
                {
                    lock (dataLock)
                    {
                        string cpy = string.Copy(readData);
                        Task.Run(() => PacketAvailable?.Invoke(cpy));
                        readData = "";

                        start = DateTime.MaxValue;
                    }
                }

                Thread.Sleep(1);
            }
            running = false;
        }

    }
}
