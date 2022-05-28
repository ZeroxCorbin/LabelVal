using SQLite;
using System;
using System.Collections.Generic;

namespace V275_Testing.Databases
{
    public class RunDatabase : IDisposable
    {
        public class Run
        {
            [PrimaryKey]
            public long TimeDate { get; set; } = DateTime.Now.Ticks;
         
            public string Job { get; set; }
            public string StoredReport { get; set; }

            public int LabelNumber { get; set; }
            public byte[] LabelImage { get; set; }
            public string LabelImageUID { get; set; }

            public byte[] RepeatImage { get; set; }
            public string Report { get; set; }
        }

        private SQLiteConnection Connection { get; set; } = null;

        public RunDatabase Open(string dbFilePath)
        {
            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            try
            {
                try
                {
                    if (Connection == null)
                        Connection = new SQLiteConnection(dbFilePath);
                }
                catch
                {
                    return null;
                }

                Connection.CreateTable<Run>();
                Connection.CreateTable<JobDatabase.Job>();

                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public void Close() => Connection?.Dispose();

        public int InsertOrReplace(Run run) => Connection.InsertOrReplace(run);
        public bool ExistsRun(long timeDate) => Connection.Table<Run>().Where(v => v.TimeDate == timeDate).Count() > 0;
        public Run SelectRun(long timeDate) => Connection.Table<Run>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
        public List<Run> SelectAllRuns() => Connection.CreateCommand("select * from Run").ExecuteQuery<Run>();
        public int DeleteRun(long timeDate) => Connection.Table<Run>().Delete(v => v.TimeDate == timeDate);

        public int InsertOrReplace(JobDatabase.Job job) => Connection.InsertOrReplace(job);
        public bool ExistsJob(long timeDate) => Connection.Table<JobDatabase.Job>().Where(v => v.TimeDate == timeDate).Count() > 0;
        public JobDatabase.Job SelectJob(long timeDate) => Connection.Table<JobDatabase.Job>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
        public List<JobDatabase.Job> SelectAllJobs() => Connection.CreateCommand("select * from Job").ExecuteQuery<JobDatabase.Job>();
        public int DeleteJob(long timeDate) => Connection.Table<JobDatabase.Job>().Delete(v => v.TimeDate == timeDate);

        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
