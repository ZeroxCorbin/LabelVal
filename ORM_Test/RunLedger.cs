using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ORM_Test
{
    public class RunLedger
    {
        public virtual int Id { get; protected set; }

        public virtual DateTime CreatedOn { get; set; } = DateTime.Now;

        public virtual string ComputerId { get; set; } = System.Environment.MachineName;

        public virtual string Job { get; set; }
        public virtual string Mac { get; set; }

        public virtual string SerialNumber { get; set; }

        public RunLedger() { }
        public RunLedger(string job, string mac, string serialNumber)
        {
            Job = job;
            Mac = mac;
            SerialNumber = serialNumber;
        }

    }
}
