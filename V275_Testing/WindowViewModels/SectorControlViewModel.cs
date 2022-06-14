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

        public SectorDifferenceViewModel SectorResults { get => sectorResults; set => SetProperty(ref sectorResults, value); } 
        private SectorDifferenceViewModel sectorResults = new SectorDifferenceViewModel();
        //public List<V275_Report_InspectSector_Common.Alarm> Alarms { get; } = new List<V275_Report_InspectSector_Common.Alarm>();

        public bool IsWarning { get; }
        public bool IsError { get; }

        private bool isWrongStandard;
        public bool IsWrongStandard { get => isWrongStandard; set => SetProperty(ref isWrongStandard, value); }

        private bool isGS1Standard;
        public bool IsGS1Standard { get => isGS1Standard; set => SetProperty(ref isGS1Standard, value); }

        public SectorControlViewModel(V275_Job.Sector jobSector, object reportSector, bool isWrongStandard, bool isGS1Standard)
        {
            ReportSector = reportSector;
            JobSector = jobSector;
            IsWrongStandard = isWrongStandard;
            IsGS1Standard = isGS1Standard;

            SectorResults.Process(reportSector, jobSector.username);

            int highCat = 0;

            foreach (var alm in SectorResults.Alarms)
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
