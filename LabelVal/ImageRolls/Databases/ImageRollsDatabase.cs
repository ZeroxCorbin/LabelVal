using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.ViewModels;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.ImageRolls.Databases;
public class ImageRollsDatabase
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public FileFolderEntry File { get; private set; }
    private SQLiteConnection Connection { get; set; } = null;

    public ImageRollsDatabase(FileFolderEntry fileFolderEntry) { File = fileFolderEntry; Open(); }
    private void Open()
    {
        try
        {
            Connection ??= new SQLiteConnection(File.Path);

            Connection.CreateTable<ImageRoll>();
            Connection.CreateTable<ImageEntry>();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }
    public void Close() => Connection?.Close();

    public int InsertOrReplaceImageRoll(ImageRoll rol) => Connection.InsertOrReplace(rol);
    public bool ExistsImageRoll(string uid)
    {
        try
        {
            return Connection.Table<ImageRoll>().Where(v => v.UID == uid).Count() > 0;
        }
        catch { return false; }

    }
    public ImageRoll SelectImageRoll(string uid) => Connection.Table<ImageRoll>().Where(v => v.UID == uid).FirstOrDefault();
    public List<ImageRoll> SelectAllImageRolls() => Connection.Query<ImageRoll>("select * from ImageRoll");
    public bool DeleteImageRoll(string uid) => Connection.Delete<ImageRoll>(uid) > 0;

    public int InsertOrReplaceImage(ImageEntry img) => Connection.InsertOrReplace(img);
    public bool ExistsImage(string rollUID, string imageUID) => Connection.Table<ImageEntry>().Where(v => v.UID == imageUID && v.RollUID == rollUID).Count() > 0;
    public ImageEntry SelectImage(string rollUID, string imageUID) => Connection.Table<ImageEntry>().Where(v => v.UID == imageUID && v.RollUID == rollUID).FirstOrDefault();
    public List<ImageEntry> SelectAllImages(string rollUID)
    {
        try
        {
            if (Connection.Table<ImageEntry>().Count() > 0)
                return Connection.Table<ImageEntry>().Where(v => v.RollUID == rollUID).ToList();
        }
        catch { }
        return new List<ImageEntry>();
    }
    public List<ImageEntry> SelectAllImages() => Connection.Query<ImageEntry>("select * from ImageEntry");
    public bool DeleteImage(string rollUID, string imageUID) => Connection.Execute("DELETE FROM ImageEntry WHERE UID = ? AND RollUID = ?", imageUID, rollUID) > 0;

}
