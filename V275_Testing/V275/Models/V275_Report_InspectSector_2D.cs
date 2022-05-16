using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Report_InspectSector_2D
    {

        public string name { get; set; }
        public string type { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public V275_Report_InspectSector.Alarm[] alarms { get; set; }
            public string symbolType { get; set; }
            public string decodeText { get; set; }
            public string lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string lightSource { get; set; }
            public Overallgrade overallGrade { get; set; }
            public Decode decode { get; set; }
            public V275_Report_InspectSector.GradeValue symbolContrast { get; set; }
            public Modulation modulation { get; set; }
            public V275_Report_InspectSector.GradeValue reflectanceMargin { get; set; }
            public V275_Report_InspectSector.GradeValue axialNonUniformity { get; set; }
            public V275_Report_InspectSector.GradeValue gridNonUniformity { get; set; }
            public V275_Report_InspectSector.GradeValue unusedErrorCorrection { get; set; }
            public Fixedpatterndamage fixedPatternDamage { get; set; }
            public Maximumreflectance maximumReflectance { get; set; }
            public Minimumreflectance minimumReflectance { get; set; }
            public Gs1symbolquality gs1SymbolQuality { get; set; }
            public Gs1results gs1Results { get; set; }
        }

        public class Overallgrade
        {
            public V275_Report_InspectSector.Grade grade { get; set; }
            public string _string { get; set; }
        }

        public class Decode
        {
            public V275_Report_InspectSector.Grade grade { get; set; }
        }


        public class Modulation
        {
            public V275_Report_InspectSector.Grade grade { get; set; }
        }

        public class Fixedpatterndamage
        {
            public V275_Report_InspectSector.Grade grade { get; set; }
        }

        public class Maximumreflectance
        {
            public int value { get; set; }
        }

        public class Minimumreflectance
        {
            public int value { get; set; }
        }

        public class Gs1symbolquality
        {
            public V275_Report_InspectSector.ValueResult symbolWidth { get; set; }
            public V275_Report_InspectSector.ValueResult symbolHeight { get; set; }
            public V275_Report_InspectSector.ValueResult cellSizeX { get; set; }
            public V275_Report_InspectSector.ValueResult cellSizeY { get; set; }
            public int growthX { get; set; }
            public int growthY { get; set; }
            public int formatInfo { get; set; }
            public int versionInfo { get; set; }
        }

        public class Gs1results
        {
            public bool validated { get; set; }
            public string input { get; set; }
            public string formattedOut { get; set; }
            public Fields fields { get; set; }
            public string error { get; set; }
        }

        public class Fields
        {
            public string _01 { get; set; }
            public string _90 { get; set; }
        }

        //public class Alarm
        //{
        //    public string name { get; set; }
        //    public int category { get; set; }
        //    public Data1 data { get; set; }
        //}

        public class Data1
        {
            public string text { get; set; }
            public int index { get; set; }
            public string subAlarm { get; set; }
            public string expected { get; set; }
        }

    }
}
