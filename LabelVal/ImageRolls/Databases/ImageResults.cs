using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Forms;

namespace LabelVal.ImageRolls.Databases
{
    public partial class ImageResults : ObservableObject, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string FilePath { get; private set; }
        public string FileName => System.IO.Path.GetFileNameWithoutExtension(FilePath);

        public partial class V275Result : ObservableObject
        {
            [ObservableProperty] private string imageRollName;
            [ObservableProperty] private byte[] sourceImage;
            [ObservableProperty] [property: PrimaryKey] private string sourceImageUID;
            [ObservableProperty] private string template;
            [ObservableProperty] private string report;
            [ObservableProperty] private byte[] storedImage;
        }

        public partial class V5Result : ObservableObject
        {
            [ObservableProperty] private string imageRollName;
            [ObservableProperty] private byte[] sourceImage;
            [ObservableProperty] [property: PrimaryKey] private string sourceImageUID;
            [ObservableProperty] private string template;
            [ObservableProperty] private string report;
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

        public partial class L95xxResult : ObservableObject
        {
            [ObservableProperty] private string imageRollName;
            [ObservableProperty] private byte[] sourceImage;
            [ObservableProperty][property: PrimaryKey] private string sourceImageUID;
            [ObservableProperty] private string template;
            [ObservableProperty] private string report;
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
        public ImageResults(string dbFilePath) => FilePath = dbFilePath;

        public ImageResults Open(string dbFilePath)
        {
            FilePath = dbFilePath;
            return Open();
        }

        public ImageResults Open()
        {
            Logger.Info("Opening Database: {file}", FilePath);

            if (string.IsNullOrEmpty(FilePath))
                return null;

            try
            {
                Connection ??= new SQLiteConnection(FilePath);

                _ = Connection.CreateTable<V275Result>();
                _ = Connection.CreateTable<V5Result>();
                _ = Connection.CreateTable<L95xxResult>();
                _ = Connection.CreateTable<LockTable>();

                OnPropertyChanged(nameof(IsLocked));
                OnPropertyChanged(nameof(IsNotLocked));

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


        public int? InsertOrReplace_V5Result(V5Result result) => Connection?.InsertOrReplace(result);
        public bool Exists_V5Result(string imageRollName, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).Count() > 0;
        public V5Result Select_V5Result(string imageRollName, string imageUID) => Connection?.Table<V5Result>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).FirstOrDefault();
        public List<V5Result> SelectAll_V5Result() => Connection?.Query<V5Result>("select * from V5Result");
        public int? Delete_V5Result(string imageRollName, string imageUID) => Connection?.Table<V5Result>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName);


        public int? InsertOrReplace_L95xxResult(L95xxResult result) => Connection?.InsertOrReplace(result);
        public bool Exists_L95xxResult(string imageRollName, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).Count() > 0;
        public L95xxResult Select_L95xxResult(string imageRollName, string imageUID) => Connection?.Table<L95xxResult>().Where(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName).FirstOrDefault();
        public List<L95xxResult> SelectAll_L95xxResult() => Connection?.Query<L95xxResult>("select * from L95xxResult");
        public int? Delete_L95xxResult(string imageRollName, string imageUID) => Connection?.Table<L95xxResult>().Delete(v => v.SourceImageUID == imageUID && v.ImageRollName == imageRollName);


        public List<string> AllTableNames()
        {
            using var con = new System.Data.SQLite.SQLiteConnection($"Data Source={FilePath}; Version=3;");
            con.Open();

            using System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", con);

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


        public bool NeedsConversion => NeedsConversionCheck();

        private void Lock(bool isPerminent) => Connection.InsertOrReplace(new LockTable { IsPerminent = isPerminent });
        private bool HasLock() => HasPerminentLock() || Connection?.Table<LockTable>().Count() > 0;
        private bool HasPerminentLock() => Connection?.Table<LockTable>().Where(v => v.IsPerminent).Count() > 0;
        private void Unlock() => _ = Connection.DeleteAll<LockTable>();

        [RelayCommand]
        public async Task ConvertDatabase()
        {

        }
        private bool NeedsConversionCheck()
        {
            var tables = AllTableNames();
            if (tables.Count > 3)
                return true;

            foreach (var table in tables)
            {
                if (table.Equals("V275Result", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                else if (table.Equals("V5Result", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                else if (table.Equals("LockTable", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                else if (table.Equals("L95xxResult", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
