using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    internal class V275_Job_Sector_Verify1D
    {
        public string name { get; set; }
        public string username { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int left { get; set; }
        public int top { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int angle { get; set; }
        public int orientation { get; set; }
        public bool supportMatching { get; set; }
        public Matchsettings matchSettings { get; set; }
        public string symbology { get; set; }
        public float warningGrade { get; set; }
        public float passingGrade { get; set; }
        public int apertureMode { get; set; }
        public int aperturePercent { get; set; }
        public int apertureDimension { get; set; }
        public Gradingstandard gradingStandard { get; set; }
        public string metaData { get; set; }

        public class Matchsettings
        {
            public int dataLength { get; set; }
            public string fieldMask { get; set; }
            public int mod10CheckDigit { get; set; }
            public int requireFNC1 { get; set; }
            public int matchMode { get; set; }
            public string promptUserAtStartMessage { get; set; }
            public string fixedText { get; set; }
            public string matchToSector { get; set; }
            public int matchSectorStartPosition { get; set; }
            public int stepCharSetOption { get; set; }
            public int stepDelta { get; set; }
            public string stepCharSet { get; set; }
            public int userDefinedDataOption { get; set; }
            public object[] userDefinedData { get; set; }
            public int userDefinedDataTrueSize { get; set; }
            public int duplicateCheckOption { get; set; }
            public int uniqueSetNumber { get; set; }
        }

        public class Gradingstandard
        {
            public bool enabled { get; set; }
            public string standard { get; set; }
            public string tableId { get; set; }
            public Specifications specifications { get; set; }
            public int xdimFailOption { get; set; }
            public int barheightFailOption { get; set; }
        }

        public class Specifications
        {
            public string symbology { get; set; }
            public string symbolType { get; set; }
            public int minXdim { get; set; }
            public int maxXdim { get; set; }
            public int minHeightFactor { get; set; }
            public int minHeightAbs { get; set; }
            public int minLeftQZ { get; set; }
            public int minRightQZ { get; set; }
            public int minOverallGrade { get; set; }
            public int aperture { get; set; }
        }

    }
}
