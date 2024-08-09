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
}
