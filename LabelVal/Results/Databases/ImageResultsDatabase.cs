using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Results.ViewModels;
using Org.BouncyCastle.Tls;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Results.Databases;

public class ImageResultsDatabase : ObservableObject, IDisposable
{    public FileFolderEntry File { get; private set; }
    private SQLiteConnection Connection { get; set; }

    public ImageResultsDatabase() { }
    public ImageResultsDatabase(FileFolderEntry fileEntry)
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
            Logger.LogError(e);
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
    public bool Exists_Result(ImageResultEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Where(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    public Result Select_Result(ImageResultEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Where(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault();
    public List<Result> SelectAll_Result() => Connection?.Query<Result>("select * from Result");
    public int? Delete_Result(ImageResultEntryDevices device, string imageRollUID, string imageUID, string runUID) => Connection?.Table<Result>().Delete(v => v.Device == device && v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);

    //public int? InsertOrReplace_V275Result(V275Result result)
    //{
    //    if (Exists_V275Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID))
    //    {
    //        Delete_V275Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID);
    //        return Connection?.Insert(result);
    //    }
    //    else
    //        return Connection?.Insert(result);
    //}
    //public bool Exists_V275Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    //public V275Result Select_V275Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault();
    //public List<V275Result> SelectAll_V275Result() => Connection?.Query<V275Result>("select * from V275Result");
    //public int? Delete_V275Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V275Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);

    //public int? InsertOrReplace_V5Result(V5Result result)
    //{
    //    if (Exists_V5Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID))
    //    {
    //        Delete_V5Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID);
    //        return Connection?.Insert(result);
    //    }
    //    else
    //        return Connection?.Insert(result);
    //}
    //public bool Exists_V5Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    //public V5Result Select_V5Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault();
    //public List<V5Result> SelectAll_V5Result() => Connection?.Query<V5Result>("select * from V5Result");
    //public int? Delete_V5Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<V5Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);

    //public int? InsertOrReplace_L95Result(L95Result result)
    //{
    //    if(Exists_L95Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID: result.ImageRollUID))
    //    {
    //        Delete_L95Result(result.ImageRollUID, result.SourceImageUID, !string.IsNullOrEmpty(result.RunUID) ? result.RunUID : result.ImageRollUID);
    //        return Connection?.Insert(result);
    //    }
    //    else
    //        return Connection?.Insert(result);
    //}
    //public bool Exists_L95Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<L95Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).Count() > 0;
    //public L95Result Select_L95Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<L95Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID).FirstOrDefault();
    //public List<L95Result> SelectAll_L95Result() => Connection?.Query<L95Result>("select * from L95Result");
    //public int? Delete_L95Result(string imageRollUID, string imageUID, string runUID) => Connection?.Table<L95Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID && v.RunUID == runUID);

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
