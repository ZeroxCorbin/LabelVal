using FluentNHibernate.Cfg.Db;
using Newtonsoft.Json;
using NHibernate.Driver;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace LabelVal.LVS_95xx.Controllers;
public class L95xxDatabaseConnection
{
    public OleDbDriver OleDbDriver { get; } = new OleDbDriver();

    private DbConnection Connection { get; set; }

    public bool Connect()
    {
        if (Connection != null)
            return true;

        try
        {
            JetDriverConnectionStringBuilder str = new();
            _ = str.Provider("Microsoft.Jet.OLEDB.4.0").DatabaseFile(@"C:\Users\Public\LVS-95XX\LVS-95XX.mdb");
            Connection = OleDbDriver.CreateConnection();
            string str1 = str.ToString();

            Connection.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                @"Data Source=C:\Users\Public\LVS-95XX\LVS-95XX.mdb;User ID=Admin";
            Connection.Open();
        }
        catch (Exception)
        {
            Disconnect();
            return false;
        }

        return true;
    }

    public void Disconnect()
    {
        if (Connection != null)
        {
            Connection.Close();
            Connection.Dispose();
            Connection = null;
        }
    }

    public Models.Report GetReport(string id)
    {
        if (Connection == null)
            return null;

        DbCommand cmd = OleDbDriver.CreateCommand();
        cmd.Connection = Connection;
        cmd.CommandText = $"SELECT * FROM [Reports] WHERE ReportID = {id}";

        DbDataReader reader = cmd.ExecuteReader();
        List<Dictionary<string, object>> reports = new();

        while (reader.Read())
        {
            Dictionary<string, object> report = new();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                report[reader.GetName(i)] = reader.GetValue(i);
            }
            reports.Add(report);
        }
        reader.Close();
        List<Models.Report> tmp = JsonConvert.DeserializeObject<List<Models.Report>>(JsonConvert.SerializeObject(reports));
        return tmp.Count > 0 ? tmp[0] : null;
    }

    public List<Models.ReportData> GetReportData(string id)
    {
        if (Connection == null)
            return null;

        DbCommand cmd = OleDbDriver.CreateCommand();
        cmd.Connection = Connection;
        cmd.CommandText = $"SELECT * FROM [ReportData] WHERE ReportID = {id}";

        DbDataReader reader = cmd.ExecuteReader();
        List<Dictionary<string, object>> reports = new();

        while (reader.Read())
        {
            Dictionary<string, object> report = new();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                report[reader.GetName(i)] = reader.GetValue(i);
            }
            reports.Add(report);
        }
        reader.Close();

        return JsonConvert.DeserializeObject<List<Models.ReportData>>(JsonConvert.SerializeObject(reports));
    }

    public Dictionary<string, Dictionary<string, object>> ReadSettings()
    {
        if (Connection == null)
            return null;

        DbCommand cmd = OleDbDriver.CreateCommand();
        cmd.Connection = Connection;
        cmd.CommandText = "SELECT * FROM [Settings]";

        DbDataReader reader = cmd.ExecuteReader();
        Dictionary<string, Dictionary<string, object>> settings = new();

        while (reader.Read())
        {
            string category = reader["Category"].ToString();
            string settingName = reader["SettingName"].ToString();
            object settingValue = reader["SettingValue"];

            if (!settings.ContainsKey(category))
            {
                settings[category] = new Dictionary<string, object>();
            }

            settings[category][settingName] = settingValue;
        }
        reader.Close();

        return settings;
    }

    public string ReadSetting(string category, string settingName)
    {
        if (Connection == null)
            return null;

        DbCommand cmd = OleDbDriver.CreateCommand();
        cmd.Connection = Connection;
        cmd.CommandText = "SELECT SettingValue FROM [Settings] WHERE Category = @Category AND SettingName = @SettingName";

        DbParameter categoryParam = cmd.CreateParameter();
        categoryParam.ParameterName = "@Category";
        categoryParam.Value = category;
        cmd.Parameters.Add(categoryParam);

        DbParameter nameParam = cmd.CreateParameter();
        nameParam.ParameterName = "@SettingName";
        nameParam.Value = settingName;
        cmd.Parameters.Add(nameParam);

        object settingValue = cmd.ExecuteScalar();
        return (string)settingValue;
    }

    public void WriteSetting(string category, string settingName, string settingValue)
    {
        if (Connection == null)
            return;

        try
        {
            DbCommand cmd = OleDbDriver.CreateCommand();
            cmd.Connection = Connection;
            cmd.CommandText = "SELECT COUNT(*) FROM [Settings] WHERE Category = @Category AND SettingName = @SettingName";

            DbParameter categoryParam = cmd.CreateParameter();
            categoryParam.ParameterName = "@Category";
            categoryParam.Value = category;
            cmd.Parameters.Add(categoryParam);

            DbParameter nameParam = cmd.CreateParameter();
            nameParam.ParameterName = "@SettingName";
            nameParam.Value = settingName;
            cmd.Parameters.Add(nameParam);

            int count = (int)cmd.ExecuteScalar();

            if (count > 0)
            {
                // Update existing setting
                cmd.CommandText = "UPDATE [Settings] SET SettingValue=@SettingValue WHERE Category=@Category AND SettingName=@SettingName";
            }
            else
            {
                // Insert new setting
                cmd.CommandText = "INSERT INTO [Settings] (Category, SettingName, SettingValue) VALUES (@Category, @SettingName, @SettingValue)";
            }

            DbParameter valueParam = cmd.CreateParameter();
            valueParam.ParameterName = "@SettingValue";
            valueParam.Value = settingValue;
            cmd.Parameters.Add(valueParam);

            int res = cmd.ExecuteNonQuery();
            if (res == 0)
            {
                LogInfo("No rows were affected. SQL Command: " + cmd.CommandText);
                foreach (DbParameter p in cmd.Parameters)
                {
                    LogInfo($"Parameter: {p.ParameterName}, Value: {p.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            LogError("An error occurred: " + ex.Message, ex);
        }
    }


    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
