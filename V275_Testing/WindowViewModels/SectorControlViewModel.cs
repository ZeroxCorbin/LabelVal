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

        public V275_Job.Sector JobSector { get => jobSector; set => SetProperty(ref jobSector, value); }
        private V275_Job.Sector jobSector;
        public object ReportSector { get => reportSector; set => SetProperty(ref reportSector, value); }
        private object reportSector;
        public V275_Report_InspectSector_Compare CompareSector { get => compareSector; set => SetProperty(ref compareSector, value); } 
        private V275_Report_InspectSector_Compare compareSector = new V275_Report_InspectSector_Compare();
        //public List<V275_Report_InspectSector_Common.Alarm> Alarms { get; } = new List<V275_Report_InspectSector_Common.Alarm>();

        public bool IsWarning { get; }
        public bool IsError { get; }

        public bool IsWrongStandard { get; set; }

        public SectorControlViewModel(V275_Job.Sector jobSector, object reportSector, bool isWrongStandard)
        {
            ReportSector = reportSector;
            JobSector = jobSector;
            IsWrongStandard = isWrongStandard;

            CompareSector.Process(reportSector);

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
