
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases;

public class ResultDatabase : IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public class Result : ObservableObject
    {

        private long timeDate = DateTime.Now.Ticks;
        [PrimaryKey]
        public long TimeDate { get => timeDate; set => SetProperty(ref timeDate, value); }

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

    public ResultDatabase Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<Result>();
            _ = Connection.CreateTable<LedgerDatabase.LedgerEntry>();

            return this;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return null;
        }
    }
    public void Close() => Connection?.Dispose();

    public int InsertOrReplace(Result run) => Connection.InsertOrReplace(run);
    public bool ExistsRun(long timeDate) => Connection.Table<Result>().Where(v => v.TimeDate == timeDate).Count() > 0;
    public Result SelectRun(long timeDate) => Connection.Table<Result>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
    public List<Result> SelectAllRuns() => Connection.CreateCommand("select * from Result").ExecuteQuery<Result>();
    public int DeleteRun(long timeDate) => Connection.Table<Result>().Delete(v => v.TimeDate == timeDate);

    public int InsertOrReplace(LedgerDatabase.LedgerEntry entry) => Connection.InsertOrReplace(entry);
    public bool ExistsLedgerEntry(long timeDate) => Connection.Table<LedgerDatabase.LedgerEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
    public LedgerDatabase.LedgerEntry SelectLedgerEntry(long timeDate) => Connection.Table<LedgerDatabase.LedgerEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
    public List<LedgerDatabase.LedgerEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from LedgerEntry").ExecuteQuery<LedgerDatabase.LedgerEntry>();
    public int DeleteLedgerEntry(long timeDate) => Connection.Table<LedgerDatabase.LedgerEntry>().Delete(v => v.TimeDate == timeDate);

    public void Dispose()
    {
        Connection?.Close();
        Connection?.Dispose();
    }
}
