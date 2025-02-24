using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases;

public partial class ResultsDatabase : IDisposable
{
    private bool disposedValue;

    private SQLiteConnection Connection { get; set; } = null;

    public ResultsDatabase() { }
    public ResultsDatabase(string dbFilePath) => Open(dbFilePath);
    public ResultsDatabase Open(string dbFilePath)
    {
        LogInfo($"Opening Database: {dbFilePath}");

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
            LogError(e);
            return null;
        }
    }
    public void Close() => Connection?.Dispose();

    public int InsertOrReplace(RunEntry entry) => Connection.InsertOrReplace(entry);
    public bool ExistsLedgerEntry(string uid) => Connection.Table<RunEntry>().Where(v => v.StartTime.ToString() == uid).Count() > 0;
    public RunEntry SelectLedgerEntry(string uid) => Connection.Table<RunEntry>().Where(v => v.StartTime.ToString() == uid).FirstOrDefault();
    public List<RunEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from RunEntry").ExecuteQuery<RunEntry>();
    public int DeleteLedgerEntry(string uid) => Connection.Table<RunEntry>().Delete(v => v.StartTime.ToString() == uid);

    public int InsertOrReplace(ResultEntry irg) => Connection.InsertOrReplace(irg);
    public bool ExistsImageResultGroup(string runUID) => Connection.Table<ResultEntry>().Where(v => v.RunUID == runUID).Count() > 0;
    public ResultEntry SelectImageResultGroup(string runUID, string imageUID, int order) => Connection.Table<ResultEntry>().Where(v => v.RunUID == runUID && v.SourceImageUID == imageUID && v.Order == order ).FirstOrDefault();
    public List<ResultEntry> SelectAllImageResultGroups(string runUID) => [.. Connection.Table<ResultEntry>().Where(v => v.RunUID == runUID)];
    public int DeleteImageResultGroup(string runUID) => Connection.Table<ResultEntry>().Delete(v => v.RunUID == runUID);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Connection?.Dispose();
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~RunDatabase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion
}
