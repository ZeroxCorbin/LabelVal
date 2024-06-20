
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases;

public partial class RunDatabase : IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private SQLiteConnection Connection { get; set; } = null;

    public RunDatabase Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<RunEntry>();
            _ = Connection.CreateTable<ResultEntry>();
            

            return this;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return null;
        }
    }
    public void Close() => Connection?.Dispose();

    public int InsertOrReplace(RunEntry entry) => Connection.InsertOrReplace(entry);
    public bool ExistsLedgerEntry(long timeDate) => Connection.Table<RunEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
    public RunEntry SelectLedgerEntry(long timeDate) => Connection.Table<RunEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
    public List<RunEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from LedgerEntry").ExecuteQuery<RunEntry>();
    public int DeleteLedgerEntry(long timeDate) => Connection.Table<RunEntry>().Delete(v => v.TimeDate == timeDate);

    public int InsertOrReplace(ResultEntry run) => Connection.InsertOrReplace(run);
    public bool ExistsRun(long timeDate) => Connection.Table<ResultEntry>().Where(v => v.TimeDate == timeDate).Count() > 0;
    public ResultEntry SelectRun(long timeDate) => Connection.Table<ResultEntry>().Where(v => v.TimeDate == timeDate).FirstOrDefault();
    public List<ResultEntry> SelectAllRuns() => Connection.CreateCommand("select * from Result").ExecuteQuery<ResultEntry>();
    public int DeleteRun(long timeDate) => Connection.Table<ResultEntry>().Delete(v => v.TimeDate == timeDate);

    public void Dispose()
    {
        Connection?.Close();
        Connection?.Dispose();
    }
}
