using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

//using System.Windows.Forms;

namespace V275_Testing.Databases
{
    public class SimpleDatabase : IDisposable

    {
        public class SimpleSetting
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        //public class FormSettings
        //{
        //    public Size Size { get; set; } = new Size(1280, 720);
        //    public Point Location { get; set; } = new Point(0, 0);
        //    public System.Windows.Forms.FormWindowState State { get; set; } = System.Windows.Forms.FormWindowState.Normal;

        //    [Newtonsoft.Json.JsonIgnore]
        //    public bool IsLoading { get; set; } = true;

        //    public FormSettings()
        //    {
        //    }

        //    public void Update(Size size, System.Windows.Forms.FormWindowState state)
        //    {
        //        if (state != System.Windows.Forms.FormWindowState.Minimized) State = state;
        //        if (state != System.Windows.Forms.FormWindowState.Normal) return;

        //        Size = size;
        //        return;
        //    }

        //    public void Update(Point location, System.Windows.Forms.FormWindowState state)
        //    {
        //        if (state != System.Windows.Forms.FormWindowState.Minimized) State = state;
        //        if (state != System.Windows.Forms.FormWindowState.Normal) return;

        //        Location = location;
        //        return;
        //    }

        //    public bool IsOnScreen()
        //    {
        //        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
        //        foreach (System.Windows.Forms.Screen screen in screens)
        //        {
        //            System.Drawing.Rectangle formRectangle = new System.Drawing.Rectangle(Location.X, Location.Y,
        //                                                     Size.Width, Size.Height);

        //            if (screen.WorkingArea.Contains(formRectangle))
        //            {
        //                return true;
        //            }
        //        }

        //        return false;
        //    }
        //}
        //public class WindowSettings : INotifyPropertyChanged
        //{
        //    public event PropertyChangedEventHandler PropertyChanged;

        //    public double Top { get; set; } = 0;
        //    public double Left { get; set; } = 0;
        //    public double Width { get; set; } = 1024;
        //    public double Height { get; set; } = 768;
        //    public System.Windows.WindowState WindowState { get; set; } = System.Windows.WindowState.Normal;

        //    //[Newtonsoft.Json.JsonIgnore]
        //    //public bool IsLoading { get; set; } = true;

        //    //public WindowSettings()
        //    //{
        //    //}

        //    //public void UpdateSize(double width, double height, System.Windows.WindowState state)
        //    //{
        //    //    if (state != System.Windows.WindowState.Minimized) State = state;
        //    //    if (state != System.Windows.WindowState.Normal) return;

        //    //    Width = width;
        //    //    Height = height;
        //    //}

        //    //public void UpdateLocation(double top, double left, System.Windows.WindowState state)
        //    //{
        //    //    if (state != System.Windows.WindowState.Minimized) State = state;
        //    //    if (state != System.Windows.WindowState.Normal) return;

        //    //    Top = top;
        //    //    Left = left;
        //    //}

        //    public bool IsOnScreen()
        //    {
        //        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
        //        foreach (System.Windows.Forms.Screen screen in screens)
        //        {
        //            System.Drawing.Rectangle formRectangle = new System.Drawing.Rectangle((int)Left * 2, (int)Top * 2,
        //                                                     (int)Width * 2, (int)Height * 2);

        //            if (screen.WorkingArea.Contains(formRectangle))
        //            {
        //                return true;
        //            }
        //        }

        //        return false;
        //    }
        //}


        private SQLiteConnection Connection { get; set; } = null;

        public string DbFilePath { get; private set; } = null;
        public string DbTableName { get; private set; } = null;

        private string CREATE_TABLE(string name) => $"CREATE TABLE IF NOT EXISTS '{name}' (id integer PRIMARY KEY AUTOINCREMENT, Key TEXT NOT NULL UNIQUE, Value DATA NULL);";

        public SimpleDatabase Init(string dbFilePath, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            return Init(dbFilePath, "SimpleDB", overwrite);
        }
        public SimpleDatabase Init(string dbFilePath, string dbTableName, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(dbFilePath))
                return null;
            if (string.IsNullOrEmpty(dbTableName))
                return null;


            DbFilePath = dbFilePath;
            DbTableName = dbTableName;

            try
            {
                CreateDatabaseFile(dbFilePath, overwrite);

                if (!OpenConnection())
                    return null;

                if (!ExistsTable(dbTableName))
                {
                    if (!CreateTable(dbTableName))
                        return null;
                }


                return this;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public string GetValue(string key, string defaultValue = "")
        {
            SimpleSetting settings = SelectSetting(key);
            return string.IsNullOrEmpty(settings.Value) ? defaultValue : settings.Value;
        }
        public T GetValue<T>(string key)
        {
            SimpleSetting settings = SelectSetting(key);
            return settings.Value == string.Empty ? default : (T)Newtonsoft.Json.JsonConvert.DeserializeObject(settings.Value, typeof(T));
        }
        public T GetValue<T>(string key, T defaultValue)
        {
            SimpleSetting settings = SelectSetting(key);
            return settings.Value == string.Empty ? defaultValue : (T)Newtonsoft.Json.JsonConvert.DeserializeObject(settings.Value, typeof(T));
        }

        public List<T> GetAllValues<T>(string key)
        {
            List<SimpleSetting> settings = SelectAllSettings(key);

            List<T> lst = new List<T>();

            foreach (SimpleSetting ss in settings)
            {
                lst.Add(ss.Value == string.Empty ? default : (T)Newtonsoft.Json.JsonConvert.DeserializeObject(ss.Value, typeof(T)));
            }
            return lst;
        }

        public void SetValue(string key, string value)
        {
            SimpleSetting set = new SimpleSetting()
            {
                Key = key,
                Value = value
            };
            _ = UpdateSetting(set);
        }
        public void SetValue<T>(string key, T value)
        {
            SimpleSetting set = new SimpleSetting()
            {
                Key = key,
                Value = Newtonsoft.Json.JsonConvert.SerializeObject(value)
            };
            _ = UpdateSetting(set);
        }

        public bool ExistsSetting(string key, string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT count(*) FROM '{tableName}' WHERE Key = '{key}';", Connection))
            {
                int count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                    return false;
                else
                    return true;
            }


        }
        public SimpleSetting GetSetting(string key)
        {
            return SelectSetting(key);
        }
        public void SetSetting(SimpleSetting setting)
        {
            _ = UpdateSetting(setting);
        }
        private int InsertSetting(SimpleSetting setting, string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;

            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"INSERT OR IGNORE INTO '{tableName}' (Key, Value) VALUES (");
            _ = sb.Append($"@Key,");
            _ = sb.Append($"@Value);");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
            {
                _ = command.Parameters.AddWithValue("Key", setting.Key);
                _ = command.Parameters.AddWithValue("Value", setting.Value);
                return command.ExecuteNonQuery();
            }


        }
        private int UpdateSetting(SimpleSetting setting, string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;

            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"UPDATE '{tableName}' SET ");
            _ = sb.Append($"Key = @Key,");
            _ = sb.Append($"Value = @Value ");
            _ = sb.Append($"WHERE Key = @Key;");

            int res = -1;
            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
            {
                _ = command.Parameters.AddWithValue("Key", setting.Key);
                _ = command.Parameters.AddWithValue("Value", setting.Value);
                res = command.ExecuteNonQuery();
            }

            if (res == 0)
                res = InsertSetting(setting);

            return res;
        }
        public SimpleSetting SelectSetting(string key, string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;
            SimpleSetting settings = new SimpleSetting();

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{tableName}' WHERE Key = '{key}';", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
                if (rdr.Read())
                {
                    settings.Key = rdr.GetString(1);
                    settings.Value = rdr.GetValue(2).ToString();
                }
            return settings;
        }
        public int DeleteSetting(string key, string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;

            using (SQLiteCommand command = new SQLiteCommand($"DELETE FROM '{tableName}' WHERE Key = '{key}';", Connection))
            {
                return command.ExecuteNonQuery();
            }

        }
        public List<SimpleSetting> SelectAllSettings(string key, string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = DbTableName;
            List<SimpleSetting> settings = new List<SimpleSetting>();

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{tableName}' WHERE Key LIKE '%{key}';", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        settings.Add(new SimpleSetting() { Key = rdr["Key"].ToString(), Value = rdr["Value"].ToString() });
                    }
                }
            }

            return settings;
        }

        private void CreateDatabaseFile(string dbFilePath, bool overwrite)
        {
            if (overwrite && File.Exists(dbFilePath))
            {
                Console.WriteLine($"Deleting map database file: {dbFilePath}");
                File.Delete(dbFilePath);
            }

            if (!File.Exists(dbFilePath))
            {
                Console.WriteLine($"Creating map database file: {dbFilePath}");
                SQLiteConnection.CreateFile(dbFilePath);
            }
        }
        private bool OpenConnection()
        {
            if (Connection == null)
                Connection = new SQLiteConnection($"Data Source='{DbFilePath}'; Version=3;");

            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();

            if (Connection.State != System.Data.ConnectionState.Open)
                return false;
            else
                return true;
        }
        public void CloseConnection() => Connection?.Close();

        public bool ExistsTable(string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = DbTableName;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';", Connection))
            {
                using (SQLiteDataReader rdr = command.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        if (rdr.FieldCount == 0)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        public bool CreateTable(string tableName)
        {
            using (SQLiteCommand command = new SQLiteCommand(CREATE_TABLE(tableName), Connection))
            {
                int res = command.ExecuteNonQuery();

            return res == 0;
            }

        }

        public Dictionary<string, string> SelectAllSettingsRows()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{DbTableName}'", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
                while (rdr.Read())
                    dict.Add(rdr.GetString(1), rdr.GetValue(2).ToString());

            return dict;
        }

        public void Dispose()
        {
            Connection?.Close();
            ((IDisposable)Connection)?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}