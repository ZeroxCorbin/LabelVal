using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Results.ViewModels;
using Org.BouncyCastle.Tls;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Results.Databases;

public class ResultsDatabase : ObservableObject, IDisposable
{    public FileFolderEntry File { get; private set; }
    private SQLiteConnection Connection { get; set; }

    public ResultsDatabase() { }
    public ResultsDatabase(FileFolderEntry fileEntry)
    {
        File = fileEntry;
        Open();
    }

    private void Open()
    {
        try
        {
            Connection ??= new SQLiteConnection(File.Path);

            _ = Connection.CreateTable<Result>();
            _ = Connection.CreateTable<Lock>();

            OnPropertyChanged(nameof(IsLocked));
            OnPropertyChanged(nameof(IsNotLocked));
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }
    public void Close() { Connection?.Close(); }

    public int? InsertOrReplace_Result(Result result)
    {
        if (Exists_Result(result.Device, result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID))
        {
            Delete_Result(result.Device, result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID);
            return Connection?.Insert(result);
        }
        else
            return Connection?.Insert(result);
    }
    public bool Exists_Result(ResultsEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Where(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    public Result Select_Result(ResultsEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Where(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault();
    public List<Result> SelectAll_Result() => Connection?.Query<Result>("select * from Result");
    public int? Delete_Result(ResultsEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Delete(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);
    public int DeleteAllResultsByRollUid(string rollUid) => Connection.Execute("DELETE FROM Result WHERE ImageRollUID = ?", rollUid);
    
    public List<string> AllTableNames()
    {
        using var con = new System.Data.SQLite.SQLiteConnection($"Data Source={File.Name}; Version=3;");
        con.Open();

        using System.Data.SQLite.SQLiteCommand command = new("SELECT name FROM sqlite_master WHERE type='table';", con);

        var tables = new List<string>();
        using (var rdr = command.ExecuteReader())
            while (rdr.Read())
                tables.Add(rdr.GetString(0));

        return tables;
    }

    public bool IsLocked
    {
        get => HasLock();
        set => OnIsDatabaseLockedChanged(value);
    }
    private void OnIsDatabaseLockedChanged(bool value)
    {
        if (HasPerminentLock())
        {
            OnPropertyChanged(nameof(IsLocked));
            OnPropertyChanged(nameof(IsNotLocked));
            return;
        }

        if (value)
            Lock(false);
        else
            Unlock();

        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(IsNotLocked));
    }
    public bool IsNotLocked => !IsLocked;

    public bool IsPermLocked
    {
        get => HasPerminentLock();
        set => OnIsPermLockedChanged(value);
    }
    private void OnIsPermLockedChanged(bool value)
    {
        if (HasPerminentLock())
        {
            OnPropertyChanged(nameof(IsPermLocked));
            OnPropertyChanged(nameof(IsNotPermLocked));
            return;
        }

        if (value)
            Lock(true);

        OnPropertyChanged(nameof(IsPermLocked));
        OnPropertyChanged(nameof(IsNotPermLocked));
    }
    public bool IsNotPermLocked => !IsPermLocked;

    private void Lock(bool isPerminent) => Connection.InsertOrReplace(new Lock { IsPerminent = isPerminent });
    private bool HasLock() => Connection?.Table<Lock>().Count() > 0 ? HasPerminentLock() : false;
    private bool HasPerminentLock() => Connection?.Table<Lock>().Count() > 0 ? Connection?.Table<Lock>().Where(v => v.IsPerminent).Count() > 0 : false;
    private void Unlock() => _ = Connection.DeleteAll<Lock>();

    public void Dispose()
    {
        Connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
