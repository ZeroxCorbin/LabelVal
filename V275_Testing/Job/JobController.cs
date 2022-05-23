using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.Databases;

namespace V275_Testing.Job
{
    public class JobController
    {
        public long TimeDate { get; private set; }
        public JobDatabase Jobs {get; private set; }

        public JobDatabase.Job Job {get; private set; }
        
        public RunDatabase Runs { get; private set; }

        

        public JobController(string gradingStandard)
        {
            TimeDate = DateTime.UtcNow.Ticks;

            OpenDatabases();
            CreateJobEntries();
        }

        public JobController(long timeDate)
        {
            TimeDate = timeDate;

            OpenDatabases();
        }

        private void OpenDatabases()
        {
            Jobs = new JobDatabase().Open($"{App.JobsRoot}\\{App.JobsDatabaseName}");
            Runs = new RunDatabase().Open($"{App.JobsRoot}\\{App.RunsDatabaseName(TimeDate)}");

        }
        private void CreateJobEntries()
        {
            Job = new JobDatabase.Job() { };
            Jobs.InsertOrReplace(Job);

            
        }
    }
}
