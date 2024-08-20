using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Results.ViewModels;
using Org.BouncyCastle.Tls;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Results.Databases;

public class ImageResultsDatabase : ObservableObject, IDisposable
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public FileFolderEntry File { get; private set; }
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

            _ = Connection.CreateTable<V275Result>();
            _ = Connection.CreateTable<V5Result>();
            _ = Connection.CreateTable<L95xxResult>();

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

    public int? InsertOrReplace_V275Result(V275Result result) => Connection?.InsertOrReplace(result);
    public bool Exists_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    public V275Result Select_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    public List<V275Result> SelectAll_V275Result() => Connection?.Query<V275Result>("select * from V275Result");
    public int? Delete_V275Result(string imageRollUID, string imageUID) => Connection?.Table<V275Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

    public int? InsertOrReplace_V5Result(V5Result result) => Connection?.InsertOrReplace(result);
    public bool Exists_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    public V5Result Select_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    public List<V5Result> SelectAll_V5Result() => Connection?.Query<V5Result>("select * from V5Result");
    public int? Delete_V5Result(string imageRollUID, string imageUID) => Connection?.Table<V5Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

    public int? InsertOrReplace_L95xxResult(L95xxResult result) => Connection?.InsertOrReplace(result);
    public bool Exists_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).Count() > 0;
    public L95xxResult Select_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID).FirstOrDefault();
    public List<L95xxResult> SelectAll_L95xxResult() => Connection?.Query<L95xxResult>("select * from L95xxResult");
    public int? Delete_L95xxResult(string imageRollUID, string imageUID) => Connection?.Table<L95xxResult>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollUID == imageRollUID);

    public List<string> AllTableNames()
    {
        using var con = new System.Data.SQLite.SQLiteConnection($"Data Source={File.Name}; Version=3;");
        con.Open();

        using System.Data.SQLite.SQLiteCommand command = new("SELECT name FROM sqlite_master WHERE type='table';", con);

        var tables = new List<string>();
        using (System.Data.SQLite.SQLiteDataReader rdr = command.ExecuteReader())
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
    private bool HasLock() => HasPerminentLock() || Connection?.Table<Lock>().Count() > 0;
    private bool HasPerminentLock() => Connection?.Table<Lock>().Where(v => v.IsPerminent).Count() > 0;
    private void Unlock() => _ = Connection.DeleteAll<Lock>();

    public void Dispose()
    {
        Connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
