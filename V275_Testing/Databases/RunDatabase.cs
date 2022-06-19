
using SQLite;
using System;
using System.Collections.Generic;

namespace V275_Testing.Databases
{
    public class RunDatabase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class Run : Core.BaseViewModel
        {

            private long timeDate = DateTime.Now.Ticks;
            [PrimaryKey]
            public long TimeDate { get => this.timeDate; set => SetProperty(ref timeDate, value); }

            private int loopCount;
            public int LoopCount { get => loopCount; set => SetProperty(ref loopCount, value); }

            private int labelImageOrder;
            public int LabelImageOrder { get => labelImageOrder; set => SetProperty(ref labelImageOrder, value); }

            private string labelImageUID;
            public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

            private byte[] labelImage;
            public byte[] LabelImage { get => labelImage; set => SetProperty(ref labelImage, value); }

            private string labelTemplate;
            public string LabelTemplate { get => labelTemplate; set => SetProperty(ref labelTemplate, value); }

            private string labelReport;
            public string LabelReport { get => labelReport; set => SetProperty(ref labelReport, value); }


            private byte[] repeatImage;
            public byte[] RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

            private byte[] repeatGoldenImage;
            public byte[] RepeatGoldenImage { get => repeatGoldenImage; set => SetProperty(ref repeatGoldenImage, value); }

            private string repeatReport;
            public string RepeatReport { get => repeatReport; set => SetProperty(ref repeatReport, value); }

        }

        private SQLiteConnection Connection { get; set; } = null;

        public RunDatabase Open(string dbFilePath)
        {
            Logger.Info("Opening Database: {file}", dbFilePath);

            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            try
            {
                if (Connection == null)
                    Connection = new SQLiteConnection(dbFilePath);

                Connection.CreateTable<Run>();
                Connection.CreateTable<RunLedgerDatabase.RunEntry>();

                return this;
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
