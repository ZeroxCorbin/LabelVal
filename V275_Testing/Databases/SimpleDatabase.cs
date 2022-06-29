using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

//using System.Windows.Forms;

namespace LabelVal.Databases
{
    public class SimpleDatabase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class SimpleSetting
        {
            [PrimaryKey]
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private SQLiteConnection Connection { get; set; } = null;

        public SimpleDatabase Open(string dbFilePath)
        {
            Logger.Info("Opening Database: {file}", dbFilePath);

            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            try
            {
                if (Connection == null)
                    Connection = new SQLiteConnection(dbFilePath);

                Connection.CreateTable<SimpleSetting>();

                return this;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        public string GetValue(string key, string defaultValue = "")
        {
            SimpleSetting settings = SelectSetting(key);
            return settings == null ? defaultValue : settings.Value;
        }
        public T GetValue<T>(string key)
        {
            SimpleSetting settings = SelectSetting(key);
            return settings == null ? default : (T)Newtonsoft.Json.JsonConvert.DeserializeObject(settings.Value, typeof(T));
        }
        public T GetValue<T>(string key, T defaultValue)
        {
            SimpleSetting settings = SelectSetting(key);
            return settings == null ? defaultValue : (T)Newtonsoft.Json.JsonConvert.DeserializeObject(settings.Value, typeof(T));
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
            _ = InsertOrReplace(set);
        }
        public void SetValue<T>(string key, T value)
        {
            SimpleSetting set = new SimpleSetting()
            {
                Key = key,
                Value = Newtonsoft.Json.JsonConvert.SerializeObject(value)
            };
            _ = InsertOrReplace(set);
        }

        public bool ExistsSetting(string key)
        {
            return Connection.Table<SimpleSetting>().Where(v => v.Key == key).Count() > 0;
        }
 
        private int InsertOrReplace(SimpleSetting setting) => Connection.InsertOrReplace(setting);
        public SimpleSetting SelectSetting(string key)=> Connection.Table<SimpleSetting>().Where(v => v.Key == key).FirstOrDefault();
        public int DeleteSetting(string key) => Connection.Table<SimpleSetting>().Delete(v => v.Key == key);
        public List<SimpleSetting> SelectAllSettings(string key) => Connection.CreateCommand("select * from SimpleSetting").ExecuteQuery<SimpleSetting>();

        public void Close() => Connection?.Dispose();
        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}