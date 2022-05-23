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
        public class Job
        {
            [PrimaryKey]
            public long TimeDate { get; set; }

            public int Completed { get; set; }
            public string GradingStandard { get; set; }
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
                catch
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
        public void Close() => Connection?.Close();

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
            ((IDisposable)Connection)?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}


