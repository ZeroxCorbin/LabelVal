using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using V275_Testing.Databases;
using V275_Testing.V275.Models;
using V275_Testing.WindowViewModels;

namespace V275_Testing.RunViewModels
{
    public class RunLabelControlViewModel : Core.BaseViewModel
    {
        private RunDatabase.Run run;
        public RunDatabase.Run Run { get => run; set => SetProperty(ref run, value); }
                
        private RunLedgerDatabase.RunEntry runEntry;
        public RunLedgerDatabase.RunEntry RunEntry { get => runEntry; set => SetProperty(ref runEntry, value); }
        
        private ObservableCollection<SectorControlViewModel> repeatSectors = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> RepeatSectors { get => repeatSectors; set => SetProperty(ref repeatSectors, value); }

        private ObservableCollection<SectorControlViewModel> labelSectors = new ObservableCollection<SectorControlViewModel>();
        public ObservableCollection<SectorControlViewModel> LabelSectors { get => labelSectors; set => SetProperty(ref labelSectors, value); }

        private ObservableCollection<SectorResultsViewModel> diffSectors = new ObservableCollection<SectorResultsViewModel>();
        public ObservableCollection<SectorResultsViewModel> DiffSectors { get => diffSectors; set => SetProperty(ref diffSectors, value); }

        private IDialogCoordinator dialogCoordinator;
        public RunLabelControlViewModel(IDialogCoordinator diag, RunDatabase.Run run, RunLedgerDatabase.RunEntry runEntry)
        {
            dialogCoordinator = diag;
            Run = run;
            RunEntry = runEntry;

            GetLabelSectors();
            GetRepeatSectors();
            GetSectorDiff();
        }

        private void GetLabelSectors()
        {
            LabelSectors.Clear();

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            if (!string.IsNullOrEmpty(Run.LabelReport) && !string.IsNullOrEmpty(Run.LabelTemplate))
                foreach (var jSec in JsonConvert.DeserializeObject<V275_Job>(Run.LabelTemplate).sectors)
                {
                    bool isWrongStandard = false;
                    if (jSec.type == "verify1D" || jSec.type == "verify2D")
                        if (jSec.gradingStandard.enabled)
                            isWrongStandard = !(RunEntry.GradingStandard == $"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}");
                        else
                            isWrongStandard = true;

                    foreach (JObject rSec in JsonConvert.DeserializeObject<V275_Report>(Run.LabelReport).inspectLabel.inspectSector)
                    {
                        if (jSec.name == rSec["name"].ToString())
                        {

                            object fSec = DeserializeSector(rSec);

                            if (fSec == null)
                                break;

                            tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));

                            break;
                        }
                    }
                }

            if (tempSectors.Count > 0)
            {
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    LabelSectors.Add(sec);
            }
        }

        private void GetRepeatSectors()
        {
            RepeatSectors.Clear();

            List<SectorControlViewModel> tempSectors = new List<SectorControlViewModel>();
            if (!string.IsNullOrEmpty(Run.RepeatReport) && !string.IsNullOrEmpty(Run.LabelTemplate))
                foreach (var jSec in JsonConvert.DeserializeObject<V275_Job>(Run.LabelTemplate).sectors)
                {
                    bool isWrongStandard = false;
                    if (jSec.type == "verify1D" || jSec.type == "verify2D")
                        if (jSec.gradingStandard.enabled)
                            isWrongStandard = !(RunEntry.GradingStandard == $"{jSec.gradingStandard.standard} TABLE {jSec.gradingStandard.tableId}");
                        else
                            isWrongStandard = true;

                    foreach (JObject rSec in JsonConvert.DeserializeObject<V275_Report>(Run.RepeatReport).inspectLabel.inspectSector)
                    {
                        if (jSec.name == rSec["name"].ToString())
                        {

                            object fSec = DeserializeSector(rSec);

                            if (fSec == null)
                                break;

                            tempSectors.Add(new SectorControlViewModel(jSec, fSec, isWrongStandard));

                            break;
                        }
                    }
                }

            if (tempSectors.Count > 0)
            {
                tempSectors = tempSectors.OrderBy(x => x.JobSector.top).ToList();

                foreach (var sec in tempSectors)
                    RepeatSectors.Add(sec);
            }
        }

        private void GetSectorDiff()
        {
            List<SectorResultsViewModel> diff = new List<SectorResultsViewModel>();
            foreach (var sec in LabelSectors)
            {
                bool found = false;
                foreach (var cSec in RepeatSectors)
                    if (sec.JobSector.name == cSec.JobSector.name)
                    {
                        found = true;
                        diff.Add(sec.SectorResults.Compare(cSec.SectorResults));
                        continue;
                    }

                if (!found)
                {
                    var dat = sec.SectorResults.Compare(new SectorResultsViewModel());
                    dat.IsSectorMissing = true;
                    diff.Add(dat);
                }

            }

            foreach(var d in diff)
                DiffSectors.Add(d);

        }

        private object DeserializeSector(JObject reportSec)
        {
            if (reportSec["type"].ToString() == "verify1D")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify1D>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "verify2D")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Verify2D>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "ocr")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_OCR>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "ocv")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_OCV>(reportSec.ToString());
            }
            else if (reportSec["type"].ToString() == "blemish")
            {
                return JsonConvert.DeserializeObject<V275_Report_InspectSector_Blemish>(reportSec.ToString());
            }
            else
                return null;
        }

        public void Clear()
        {
            RepeatSectors.Clear();
            RepeatSectors = null;

            LabelSectors.Clear();
            LabelSectors = null;
            
            //LabelImage.StreamSource = null;
            //LabelImage = null;

            //RepeatImage.StreamSource = null;
            //RepeatImage = null;

            Run = null;
            RunEntry = null;

            
            GC.Collect();
        }

    }
}
