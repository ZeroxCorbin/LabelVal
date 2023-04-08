using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ORM_Test
{
    public class Report
    {
        public Report() { }
        public Report(V275_REST_lib.Models.Report report)
        {
            repeat = report.inspectLabel.repeat;
            voidRepeat = report.inspectLabel.voidRepeat;
            iteration = report.inspectLabel.iteration;
            result = report.inspectLabel.result;
            width = report.inspectLabel.width;
            height = report.inspectLabel.height;

            userAction = JsonConvert.SerializeObject(report.inspectLabel.userAction);

            inspectSector = JsonConvert.SerializeObject(report.inspectLabel.inspectSector);
            ioLines = JsonConvert.SerializeObject(report.inspectLabel.ioLines);
        }

        public virtual int id { get; protected set; }

        public virtual int repeat { get; set; }
        public virtual int voidRepeat { get; set; }
        public virtual int iteration { get; set; }
        public virtual string result { get; set; }
        public virtual int width { get; set; }
        public virtual int height { get; set; }

        //This is the serialized userAction class
        public virtual string userAction { get; set; }

        //This is JSON serialized inspection results for all sector types.
        public virtual string inspectSector { get; set; }

        //This is JSON serialized ioLines defined in the template
        public virtual string ioLines { get; set; }

        public virtual byte[] repeatImage { get; set; }

    }
}
