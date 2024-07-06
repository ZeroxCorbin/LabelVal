using LabelVal.Results.Databases;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases;

public partial class RunDatabase : IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private bool disposedValue;

    private SQLiteConnection Connection { get; set; } = null;

    public RunDatabase() { }
    public RunDatabase(string dbFilePath) { _ = Open(dbFilePath); }
    public RunDatabase Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<RunEntry>();

            _ = Connection.CreateTable<StoredImageResultGroup>();
            _ = Connection.CreateTable<CurrentImageResultGroup>();

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
    public bool ExistsLedgerEntry(string uid) => Connection.Table<RunEntry>().Where(v => v.UID == uid).Count() > 0;
    public RunEntry SelectLedgerEntry(string uid) => Connection.Table<RunEntry>().Where(v => v.UID == uid).FirstOrDefault();
    public List<RunEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from LedgerEntry").ExecuteQuery<RunEntry>();
    public int DeleteLedgerEntry(string uid) => Connection.Table<RunEntry>().Delete(v => v.UID == uid);

    public int InsertOrReplace(CurrentImageResultGroup cirg) => Connection.InsertOrReplace(cirg);
    public bool ExistsCurrentImageResultGroup(string runUID) => Connection.Table<CurrentImageResultGroup>().Where(v => v.RunUID == runUID).Count() > 0;
    public CurrentImageResultGroup SelectCurrentImageResultGroup(string runUID, string imageUID) => Connection.Table<CurrentImageResultGroup>().Where(v => v.RunUID == runUID && v.SourceImageUID == imageUID ).FirstOrDefault();
    public List<CurrentImageResultGroup> SelectAllCurrentImageResultGroups(string runUID) => [.. Connection.Table<CurrentImageResultGroup>().Where(v => v.RunUID == runUID)];
    public int DeleteCurrentImageResultGroup(string runUID) => Connection.Table<CurrentImageResultGroup>().Delete(v => v.RunUID == runUID);

    public int InsertOrReplace(StoredImageResultGroup sirg) => Connection.InsertOrReplace(sirg);
    public bool ExistsStoredImageResultGroup(string runUID) => Connection.Table<StoredImageResultGroup>().Where(v => v.RunUID == runUID).Count() > 0;
    public StoredImageResultGroup SelectStoredImageResultGroup(string runUID, string imageUID) => Connection.Table<StoredImageResultGroup>().Where(v => v.RunUID == runUID && v.SourceImageUID == imageUID).FirstOrDefault();
    public List<StoredImageResultGroup> SelectAllStoredImageResultGroups(string runUID) => [.. Connection.Table<StoredImageResultGroup>().Where(v => v.RunUID == runUID)];
    public int DeleteStoredImageResultGroup(string runUID) => Connection.Table<StoredImageResultGroup>().Delete(v => v.RunUID == runUID);

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
}
