using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_Job_Sector_Blemish
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
        public bool supportMatching { get; set; }
        public int separation { get; set; }
        public int reduction { get; set; }
        public int warningPercent { get; set; }
        public int maximumIgnoreErrors { get; set; }
        public int maxThumbnailsPerSector { get; set; }
        public string unitMeasure { get; set; }
        public int dilation { get; set; }
        public Foreground foreground { get; set; }
        public Background background { get; set; }
        public Matrix matrix { get; set; }
        public Diecut dieCut { get; set; }
        public Goldenimage goldenImage { get; set; }
        public string metaData { get; set; }

        public Mask mask { get; set; }

        public class Foreground
        {
            public int sensitivity { get; set; }
            public float maximumDimension { get; set; }
            public float maximumArea { get; set; }
        }

        public class Background
        {
            public int sensitivity { get; set; }
            public float maximumDimension { get; set; }
            public float maximumArea { get; set; }
        }

        public class Matrix
        {
            public int sensitivity { get; set; }
            public float maximumDimension { get; set; }
            public float maximumArea { get; set; }
        }

        public class Diecut
        {
            public int sensitivity { get; set; }
            public float maximumDimension { get; set; }
            public float maximumArea { get; set; }
        }

        public class Goldenimage
        {
        }

        public class Mask
        {
            public int width { get; set; }
            public int height { get; set; }
            public State[] states { get; set; }
            public Layer[] layers { get; set; }
        }

        public class State
        {
            public string name { get; set; }
            public int value { get; set; }
            public int layer { get; set; }
        }

        public class Layer
        {
            public int value { get; set; }
            public int[] runLengthEncode { get; set; }
        }

    }
}
