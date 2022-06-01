using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

//using System.Windows.Forms;

namespace V275_Testing.Databases
{
    public class JobDatabase : IDisposable
    {
        public class Job : Core.BaseViewModel
        {

            private long timeDate;
            [PrimaryKey]
            public long TimeDate { get => timeDate; set => SetProperty(ref timeDate, value); }

            private int completed;
            public int Completed { get => completed; set => SetProperty(ref completed, value); }

            private string gradingStandard;
            public string GradingStandard { get => gradingStandard; set => SetProperty(ref gradingStandard, value); }

            private string productPart;
            public string ProductPart { get => productPart; set => SetProperty(ref productPart, value); }

            private string cameraMAC;
            public string CameraMAC { get => cameraMAC; set => SetProperty(ref cameraMAC, value); }

            private bool runDBMissing;
            [Ignore]
            public bool RunDBMissing { get => runDBMissing; set => SetProperty(ref runDBMissing, value); }
        }

        private SQLiteConnection Connection { get; set; } = null;

        public JobDatabase Open(string dbFilePath)
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
                catch (Exception ex)
                {
                    return null;
                }

                Connection.CreateTable<Job>();

                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public void Close() => Connection?.Dispose();

        public int InsertOrReplace(Job job) => Connection.InsertOrReplace(job);
        public bool ExistsJob(long timeDate)
        {
            return Connection.Table<Job>().Where(v => v.TimeDate == timeDate).Count() > 0;
        }
        public Job SelectJob(long timeDate) => Connection.Table<Job>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
        public List<Job> SelectAllJobs() => Connection.CreateCommand("select * from Job").ExecuteQuery<Job>();
        public int DeleteJob(long timeDate) => Connection.Table<Job>().Delete(v => v.TimeDate == timeDate);

        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}


