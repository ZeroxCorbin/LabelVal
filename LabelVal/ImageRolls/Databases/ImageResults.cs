using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.ImageRolls.Databases
{
    public partial class ImageResults : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string filePath;
        public partial class V275Result : ObservableObject
        {
            [ObservableProperty] private string imageRollName;
            [ObservableProperty] private byte[] sourceImage;
            [ObservableProperty] private string sourceImageUID;
            [ObservableProperty] private string sourceImageTemplate;
            [ObservableProperty] private string sourceImageReport;
            [ObservableProperty] private byte[] storedImage;

            //public Result(SQLiteDataReader rdr)
            //{
            //    for (int i = 0; i < rdr.FieldCount; i++)
            //    {
            //        if (rdr.GetName(i).Equals("LabelImage", StringComparison.InvariantCultureIgnoreCase))
            //            LabelImage = (byte[])rdr["LabelImage"];
            //    }

            //    LabelImageUID = rdr["LabelImageUID"].ToString();
            //    LabelTemplate = rdr["LabelTemplate"].ToString();
            //    LabelReport = rdr["LabelReport"].ToString();
            //    RepeatImage = (byte[])rdr["RepeatImage"];
            //}

        }

        public partial class V5Result : ObservableObject
        {
            [ObservableProperty] private string imageRollName;
            [ObservableProperty] private byte[] sourceImage;
            [ObservableProperty] private string sourceImageUID;
            [ObservableProperty] private string sourceImageTemplate;
            [ObservableProperty] private string sourceImageReport;
            [ObservableProperty] private byte[] storedImage;

            //public Result(SQLiteDataReader rdr)
            //{
            //    for (int i = 0; i < rdr.FieldCount; i++)
            //    {
            //        if (rdr.GetName(i).Equals("LabelImage", StringComparison.InvariantCultureIgnoreCase))
            //            LabelImage = (byte[])rdr["LabelImage"];
            //    }

            //    LabelImageUID = rdr["LabelImageUID"].ToString();
            //    LabelTemplate = rdr["LabelTemplate"].ToString();
            //    LabelReport = rdr["LabelReport"].ToString();
            //    RepeatImage = (byte[])rdr["RepeatImage"];
            //}

        }

        public partial class LockTable : ObservableObject
        {
            [ObservableProperty] private bool isPerminent;
        }


        private SQLiteConnection Connection { get; set; } = null;

        public ImageResults() { }
        public ImageResults(string dbFilePath) => filePath = dbFilePath;

        public ImageResults Open(string dbFilePath)
        {
            Logger.Info("Opening Database: {file}", dbFilePath);

            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            filePath = dbFilePath;

            try
            {
                Connection ??= new SQLiteConnection(dbFilePath);

                _ = Connection.CreateTable<V275Result>();
                _ = Connection.CreateTable<V5Result>();
                _ = Connection.CreateTable<LockTable>();

                return this;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public ImageResults Open()
        {
            Logger.Info("Opening Database: {file}", filePath);

            if (string.IsNullOrEmpty(filePath))
                return null;

            try
            {
                Connection ??= new SQLiteConnection(filePath);

                _ = Connection.CreateTable<V275Result>();
                _ = Connection.CreateTable<V5Result>();
                _ = Connection.CreateTable<LockTable>();

                return this;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }
        public void Close() => Connection?.Close();

        public int? InsertOrReplace_V275Result(V275Result result) => Connection?.InsertOrReplace(result);
        public bool Exists_V275Result(string imageRollName, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).Count() > 0;
        public V275Result Select_V275Result(string imageRollName, string imageUID) => Connection?.Table<V275Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).FirstOrDefault();
        public List<V275Result> SelectAll_V275Result() => Connection?.Query<V275Result>("select * from V275Result");
        public int? Delete_V275Result(string imageRollName, string imageUID) => Connection?.Table<V275Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName);

        public bool IsLocked
        {
            get => HasLock();
            set => OnIsDatabaseLockedChanged(value);
        }
        private void OnIsDatabaseLockedChanged(bool value)
        {
            if (HasPerminentLock()) return;

            if (value)
                Lock(false);
            else
                Unlock();
        }
        public bool IsNotLocked => !IsLocked;


        public bool IsPermLocked
        {
            get => HasPerminentLock();
            set => OnIsPermLockedChanged(value);
        }
        private void OnIsPermLockedChanged(bool value)
        {
            if (HasPerminentLock()) return;

            if (value)
                Lock(true);
        }
        public bool IsNotPermLocked => !IsPermLocked;

        public void Lock(bool isPerminent) => Connection.InsertOrReplace(new LockTable { IsPerminent = isPerminent });
        public bool HasLock() => HasPerminentLock() || Connection?.Table<LockTable>().Count() > 0;
        private bool HasPerminentLock() => Connection?.Table<LockTable>().Where(v => v.IsPerminent).Count() > 0;

        public void Unlock()
        {
            _ = Connection.DropTable<LockTable>();
        }

        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
