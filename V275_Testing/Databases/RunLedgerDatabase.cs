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
    public class RunLedgerDatabase : IDisposable
    {
        public class RunEntry : Core.BaseViewModel
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

        public RunLedgerDatabase Open(string dbFilePath)
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

                Connection.CreateTable<RunEntry>();

                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public int InsertOrReplace(RunEntry entry) => Connection.InsertOrReplace(entry);
        public bool ExistsRunEntry(long timeDate) => Connection.Table<RunEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
        public RunEntry SelectRunEntry(long timeDate) => Connection.Table<RunEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
        public List<RunEntry> SelectAllRunEntrys() => Connection.CreateCommand("select * from RunEntry").ExecuteQuery<RunEntry>();
        public int DeleteRunEntry(long timeDate) => Connection.Table<RunEntry>().Delete(v => v.TimeDate == timeDate);

        public void Close() => Connection?.Dispose();
        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}


