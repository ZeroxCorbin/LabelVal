using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    internal class V275_Report_Verify2D
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
            public V275_Report.Alarm[] alarms { get; set; }
            public string symbolType { get; set; }
            public string decodeText { get; set; }
            public string lengthUnit { get; set; }
            public float xDimension { get; set; }
            public float aperture { get; set; }
            public string lightSource { get; set; }
            public Overallgrade overallGrade { get; set; }
            public Decode decode { get; set; }
            public Symbolcontrast symbolContrast { get; set; }
            public Modulation modulation { get; set; }
            public Reflectancemargin reflectanceMargin { get; set; }
            public Axialnonuniformity axialNonUniformity { get; set; }
            public Gridnonuniformity gridNonUniformity { get; set; }
            public Unusederrorcorrection unusedErrorCorrection { get; set; }
            public Fixedpatterndamage fixedPatternDamage { get; set; }
            public Maximumreflectance maximumReflectance { get; set; }
            public Minimumreflectance minimumReflectance { get; set; }
            public Gs1symbolquality gs1SymbolQuality { get; set; }
        }

        public class Overallgrade
        {
            public Grade grade { get; set; }
            public string _string { get; set; }
        }

        public class Grade
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Decode
        {
            public Grade1 grade { get; set; }
        }

        public class Grade1
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Symbolcontrast
        {
            public Grade2 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade2
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Modulation
        {
            public Grade3 grade { get; set; }
        }

        public class Grade3
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Reflectancemargin
        {
            public Grade4 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade4
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Axialnonuniformity
        {
            public Grade5 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade5
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Gridnonuniformity
        {
            public Grade6 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade6
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Unusederrorcorrection
        {
            public Grade7 grade { get; set; }
            public int value { get; set; }
        }

        public class Grade7
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class Fixedpatterndamage
        {
            public Grade8 grade { get; set; }
        }

        public class Grade8
        {
            public float value { get; set; }
            public string letter { get; set; }
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
            public Symbolwidth symbolWidth { get; set; }
            public Symbolheight symbolHeight { get; set; }
            public Cellsizex cellSizeX { get; set; }
            public Cellsizey cellSizeY { get; set; }
            public int growthX { get; set; }
            public int growthY { get; set; }
            public L1 L1 { get; set; }
            public L2 L2 { get; set; }
            public QZL1 QZL1 { get; set; }
            public QZL2 QZL2 { get; set; }
            public OCTASA OCTASA { get; set; }
        }

        public class Symbolwidth
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Symbolheight
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Cellsizex
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class Cellsizey
        {
            public float value { get; set; }
            public string result { get; set; }
        }

        public class L1
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class L2
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class QZL1
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class QZL2
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

        public class OCTASA
        {
            public float value { get; set; }
            public string letter { get; set; }
        }

    }
}
