using SQLite;
using System;
using System.Collections.Generic;

namespace V275_Testing.Databases
{
    public class RunDatabase : IDisposable
    {
        public class Run : Core.BaseViewModel
        {

            private long timeDate = DateTime.Now.Ticks;
            [PrimaryKey]
            public long TimeDate { get => this.timeDate; set => SetProperty(ref timeDate, value); }

            private string job;
            public string Job { get => job; set => SetProperty(ref job, value); }

            private string storedReport;
            public string StoredReport { get => storedReport; set => SetProperty(ref storedReport, value); }

            private int labelNumber;
            public int LabelNumber { get => labelNumber; set => SetProperty(ref labelNumber, value); }

            private byte[] labelImage;
            public byte[] LabelImage { get => labelImage; set => SetProperty(ref labelImage, value); }

            private string labelImageUID;
            public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

            private byte[] repeatImage;
            public byte[] RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

            private string report;
            public string Report { get => report; set => SetProperty(ref report, value); }

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
                Connection.CreateTable<RunLedgerDatabase.RunEntry>();

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

        public int InsertOrReplace(RunLedgerDatabase.RunEntry entry) => Connection.InsertOrReplace(entry);
        public bool ExistsRunEntry(long timeDate) => Connection.Table<RunLedgerDatabase.RunEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
        public RunLedgerDatabase.RunEntry SelectRunEntry(long timeDate) => Connection.Table<RunLedgerDatabase.RunEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
        public List<RunLedgerDatabase.RunEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from RunEntry").ExecuteQuery<RunLedgerDatabase.RunEntry>();
        public int DeleteRunEntry(long timeDate) => Connection.Table<RunLedgerDatabase.RunEntry>().Delete(v => v.TimeDate == timeDate);

        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();
        }
    }
}
