using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.V275.Models;

namespace V275_Testing.WindowViewModels
{
    public class SectorControlViewModel : Core.BaseViewModel
    {

        public V275_Job.Sector JobSector { get; }
        public object ReportSector { get; }
        public V275_Report_InspectSector_Compare CompareSector { get; } = new V275_Report_InspectSector_Compare();

        //public List<V275_Report_InspectSector_Common.Alarm> Alarms { get; } = new List<V275_Report_InspectSector_Common.Alarm>();

        public bool IsVerify2D { get; }

        public bool IsWarning { get; }
        public bool IsError { get; }

        public bool IsWrongStandard { get; set; }

        public SectorControlViewModel(V275_Job.Sector jobSector, object reportSector, bool isWrongStandard)
        {
            ReportSector = reportSector;
            JobSector = jobSector;
            IsWrongStandard = isWrongStandard;

            CompareSector.Process(reportSector);

            if (JobSector.type == "verify2D")
                IsVerify2D = true;

            int highCat = 0;

            foreach (var alm in CompareSector.Alarms)
            {
                //Alarms.Add(alm);
                if (highCat < alm.category)
                    highCat = alm.category;
            }

            if (highCat == 1)
                IsWarning = true;
            else if (highCat == 2)
                IsError = true;

                
        }
    }
}
