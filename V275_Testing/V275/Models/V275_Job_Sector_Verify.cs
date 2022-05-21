using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Job_Sector_Verify
    {
        public string name { get; set; }
        public string username { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int left { get; set; }
        public int top { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int angle { get; set; } = 0;
        public bool supportMatching { get; set; } = true;
        public string symbology { get; set; }
        public Matchsettings matchSettings { get; set; } = new Matchsettings();
        public Gradingstandard gradingStandard { get; set; } = new Gradingstandard();
        public float warningGrade { get; set; } = 2.5f;
        public float passingGrade { get; set; } = 1.5f;
        public int orientation { get; set; }
        public string metaData { get; set; } = string.Empty;

        public class Matchsettings
        {
            public int dataLength { get; set; } = 0;
            public string fieldMask { get; set; } = String.Empty;
            public int mod10CheckDigit { get; set; } = 0;
            public int requireFNC1 { get; set; } = 0;
            public int matchMode { get; set; } = 0;
            public string promptUserAtStartMessage { get; set; } = String.Empty;
            public string fixedText { get; set; } = String.Empty;
            public string matchToSector { get; set; } = String.Empty;
            public int matchSectorStartPosition { get; set; } = 1;
            public int stepCharSetOption { get; set; } = 1;
            public int stepDelta { get; set; } = 1;
            public Stepcharset stepCharSet { get; set; } = new Stepcharset();
            public int userDefinedDataOption { get; set; } = 0;
            public object[] userDefinedData { get; set; } = new object[0];
            public int userDefinedDataTrueSize { get; set; } = 0;
            public int duplicateCheckOption { get; set; } = 2;
            public int uniqueSetNumber { get; set; } = -1;
        }

        public class Stepcharset
        {
            public string value0 { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            public string value1 { get; set; } = "0123456789";
            public string value2 { get; set; } = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        public class Gradingstandard
        {
            public bool enabled { get; set; } = false;
            public string standard { get; set; } = "GS1";
            public string tableId { get; set; } = "1";
            public int xdimFailOption { get; set; } = 0;
            public int barheightFailOption { get; set; } = 0;
            public Specifications specifications { get; set; } = new Specifications();
        }

        public class Specifications
        {
            public string symbology { get; set; } = "unknown";
            public string symbolType { get; set; } = "unknown";
            public int minXdim { get; set; } = 0;
            public int maxXdim { get; set; } = 0;
            public int minHeightFactor { get; set; } = 0;
            public int minHeightAbs { get; set; } = 0;
            public int minLeftQZ { get; set; } = 0;
            public int minRightQZ { get; set; } = 0;
            public int minOverallGrade { get; set; } = 0;
            public int aperture { get; set; } = 0;
        }

    }
}
