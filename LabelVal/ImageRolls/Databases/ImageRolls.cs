using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ImageRolls.Databases;
public class ImageRolls
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private SQLiteConnection Connection { get; set; } = null;

    public ImageRolls Open(string dbFilePath)
    {
        Logger.Info("Opening Database: {file}", dbFilePath);

        if (string.IsNullOrEmpty(dbFilePath))
            return null;

        try
        {
            if (Connection == null)
                Connection = new SQLiteConnection(dbFilePath);

            Connection.CreateTable<ImageRollEntry>();
            Connection.CreateTable<ImageEntry>();

            return this;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return null;
        }
    }
    public void Close() => Connection?.Close();

    public int InsertOrReplaceImageRoll(ImageRollEntry rol) => Connection.InsertOrReplace(rol);
    public bool ExistsImageRoll(string uid) => Connection.Table<ImageRollEntry>().Where(v => v.UID == uid).Count() > 0;
    public ImageRollEntry SelectImageRoll(string uid) => Connection.Table<ImageRollEntry>().Where(v => v.UID == uid).FirstOrDefault();
    public List<ImageRollEntry> SelectAllImageRolls() => Connection.Query<ImageRollEntry>("select * from ImageRollEntry");

    public int InsertOrReplaceImage(ImageEntry img) => Connection.InsertOrReplace(img);
    public bool ExistsImage(string rollUID, string imageUID) => Connection.Table<ImageEntry>().Where(v => v.UID == imageUID && v.RollUID == rollUID).Count() > 0;
    public ImageEntry SelectImage(string rollUID, string imageUID) => Connection.Table<ImageEntry>().Where(v => v.UID == imageUID && v.RollUID == rollUID).FirstOrDefault();
    public List<ImageEntry> SelectAllImages(string rollUID)
    {
        try
        {
            if(Connection.Table<ImageEntry>().Count() > 0)
                return Connection.Table<ImageEntry>().Where(v => v.RollUID == rollUID).ToList();
        }
        catch { }
        return new List<ImageEntry>();
    }
    public List<ImageEntry> SelectAllImages() => Connection.Query<ImageEntry>("select * from ImageEntry");

}
