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

    public RunDatabase Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            Connection ??= new SQLiteConnection(dbFilePath);

            _ = Connection.CreateTable<RunEntry>();

            _ = Connection.CreateTable<V275Result>();
            _ = Connection.CreateTable<V5Result>();
            _ = Connection.CreateTable<L95xxResult>();

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

    public int InsertOrReplace(ImageResultGroup entryGroup)
    {
        int result = 0;
        if (entryGroup.V275Result != null)
            result += Connection.InsertOrReplace(entryGroup.V275Result);
        if (entryGroup.V5Result != null)
            result += Connection.InsertOrReplace(entryGroup.V5Result);
        if (entryGroup.L95xxResult != null)
            result += Connection.InsertOrReplace(entryGroup.L95xxResult);
        return result;
    }
    public bool ExistsResultEntryGroup(string imageRollUID, string imageUID, string runUID) =>
        Connection.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0 ||
        Connection.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0 ||
        Connection.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    public ImageResultGroup SelectResultEntryGroup(string imageRollUID, string imageUID, string runUID) => new()
    {
        V275Result = Connection.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault(),
        V5Result = Connection.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault(),
        L95xxResult = Connection.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault()
    };
    public List<ImageResultGroup> SelectAllResultEntryGroups()
    {
        List<ImageResultGroup> result = [];
        foreach (var v275 in Connection.Query<V275Result>("select * from V275Result"))
            result.Add(new ImageResultGroup() { V275Result = v275 });
        foreach (var v5 in Connection.Query<V5Result>("select * from V5Result"))
            result.Add(new ImageResultGroup() { V5Result = v5 });
        foreach (var l95 in Connection.Query<L95xxResult>("select * from L95xxResult"))
            result.Add(new ImageResultGroup() { L95xxResult = l95 });
        return result;
    }
    public int DeleteResultEntryGroup(string imageRollUID, string imageUID, string runUID)
    {
        int result = 0;
        result += Connection.Table<V275Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);
        result += Connection.Table<V5Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);
        result += Connection.Table<L95xxResult>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);
        return result;
    }

    private int? InsertOrReplace_V275Result(V275Result result) => Connection?.InsertOrReplace(result);
    private bool Exists_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    private V275Result Select_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    private List<V275Result> SelectAll_V275Result() => Connection?.Query<V275Result>("select * from V275Result");
    private int? Delete_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

    private int? InsertOrReplace_V5Result(V5Result result) => Connection?.InsertOrReplace(result);
    private bool Exists_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    private V5Result Select_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    private List<V5Result> SelectAll_V5Result() => Connection?.Query<V5Result>("select * from V5Result");
    private int? Delete_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

    private int? InsertOrReplace_L95xxResult(L95xxResult result) => Connection?.InsertOrReplace(result);
    private bool Exists_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    private L95xxResult Select_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    private List<L95xxResult> SelectAll_L95xxResult() => Connection?.Query<L95xxResult>("select * from L95xxResult");
    private int? Delete_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

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
