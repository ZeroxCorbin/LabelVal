using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases;

public class LedgerDatabase : IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public class LedgerEntry : ObservableObject
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

    public LedgerDatabase Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<LedgerEntry>();

            return this;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return null;
        }
    }

    public int InsertOrReplace(LedgerEntry entry) => Connection.InsertOrReplace(entry);
    public bool ExistsLedgerEntry(long timeDate) => Connection.Table<LedgerEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
    public LedgerEntry SelectLedgerEntry(long timeDate) => Connection.Table<LedgerEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
    public List<LedgerEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from LedgerEntry").ExecuteQuery<LedgerEntry>();
    public int DeleteLedgerEntry(long timeDate) => Connection.Table<LedgerEntry>().Delete(v => v.TimeDate == timeDate);

    public void Close() => Connection?.Dispose();
    public void Dispose()
    {
        Connection?.Close();
        Connection?.Dispose();

        GC.SuppressFinalize(this);
    }
}

