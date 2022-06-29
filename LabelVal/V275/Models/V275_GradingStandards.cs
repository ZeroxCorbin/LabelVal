using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.V275.Models
{
    public class V275_GradingStandards : Core.BaseViewModel
    {
        public GradingStandard[] gradingStandards { get; set; }


        public class GradingStandard : Core.BaseViewModel
        {
            public string standard { get; set; }
            public string tableId { get; set; }
            public string description { get; set; }
            public Specifications specifications { get; set; }
        }

        public class Specifications : Core.BaseViewModel
        {
            public string symbology { get; set; }
            public string symbolType { get; set; }
            public float minXdim { get; set; }
            public float maxXdim { get; set; }
            public float minHeightFactor { get; set; }
            public float minHeightAbs { get; set; }
            public int minLeftQZ { get; set; }
            public int minRightQZ { get; set; }
            public float minOverallGrade { get; set; }
            public float aperture { get; set; }
        }

    }
}
