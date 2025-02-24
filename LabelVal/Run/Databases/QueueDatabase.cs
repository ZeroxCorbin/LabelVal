using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Run.Databases;
internal class QueueDatabase : IDisposable
{
    private bool disposedValue;

    private SQLiteConnection Connection { get; set; } = null;

    public QueueDatabase() { }
    public QueueDatabase(string dbFilePath) => Open(dbFilePath);
    public QueueDatabase Open(string dbFilePath)
    {
        LogInfo($"Opening Database: {dbFilePath}");

        if (string.IsNullOrEmpty(dbFilePath))
            return null;
        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<QueueEntry>();

            return this;
        }
        catch (Exception e)
        {
            LogError(e);
            return null;
        }
    }
    public void Close() => Connection?.Dispose();

    public int InsertOrReplace(QueueEntry entry) => Connection.InsertOrReplace(entry);
    public bool ExistsLedgerEntry(int id) => Connection.Table<QueueEntry>().Where(v => v.ID == id).Count() > 0;
    public QueueEntry SelectLedgerEntry(int id) => Connection.Table<QueueEntry>().Where(v => v.ID == id).FirstOrDefault();
    public List<QueueEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from RunEntry").ExecuteQuery<QueueEntry>();
    public int DeleteLedgerEntry(int id) => Connection.Table<QueueEntry>().Delete(v => v.ID == id);

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
