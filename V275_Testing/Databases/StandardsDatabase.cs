using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.Databases
{
    public class StandardsDatabase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class Row : Core.BaseViewModel
        {
            private string labelImageUID;
            public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

            private string labelTemplate;
            public string LabelTemplate { get => labelTemplate; set => SetProperty(ref labelTemplate, value); }  
            
            private string labelReport;
            public string LabelReport { get => labelReport; set => SetProperty(ref labelReport, value); }

            public Row(SQLiteDataReader rdr)
            { 
                LabelImageUID = rdr["LabelImageUID"].ToString();
                LabelTemplate = rdr["LabelTemplate"].ToString();
                LabelReport = rdr["LabelReport"].ToString();
            }
        }

        public string FilePath { get; private set; }

        private SQLiteConnection Connection { get; set; } = null;
        public bool IsConnectionPersistent { get; set; }



        private void CreateFile(bool overwrite = false)
        {
            if (!System.IO.File.Exists(FilePath))
            {
                Logger.Info("Creating Database: {file}", FilePath);
                SQLiteConnection.CreateFile(FilePath);
            }
        }

        private bool Open()
        {
            if (Connection == null)
                Connection = new SQLiteConnection($"Data Source={FilePath}; Version=3;");

            if (Connection.State == System.Data.ConnectionState.Closed)
            {
                Logger.Info("Opening Database: {file}", FilePath);
                Connection.Open();
            }
            
            if (Connection.State != System.Data.ConnectionState.Open)
                return false;
            else
                return true;
        }

        public void Close()
        {
            Logger.Info("Closing Database: {file}", FilePath);

            if (Connection == null)
                return;

            Connection.Close();
            Connection.Dispose();
            Connection = null;
        }

        public StandardsDatabase(string filePath, bool isConnectionPersistent = true)
        {
            FilePath = filePath;
            IsConnectionPersistent = isConnectionPersistent;

            CreateFile();
        }

        private string SerializeObject(object o) => Newtonsoft.Json.JsonConvert.SerializeObject(o, o.GetType(), new Newtonsoft.Json.JsonSerializerSettings());

        public void CreateTable(string tableName)
        {
            if (!Open()) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"CREATE TABLE '{tableName}'");
            sb.Append(" (");
            sb.Append("LabelImageUID TEXT");
            sb.Append(",");
            sb.Append("LabelTemplate TEXT");
            sb.Append(",");
            sb.Append("LabelReport TEXT");
            sb.Append(");");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
                command.ExecuteNonQuery();

            if (!IsConnectionPersistent)
                Close();
        }

        public void AddRow(string tableName, Row row) => AddRow(tableName, row.LabelImageUID, row.LabelTemplate, row.LabelReport);
        public void AddRow(string tableName, string imageUID, string template, string report)
        {
            if (!Open()) return;

            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"INSERT OR REPLACE INTO '{tableName}' (LabelImageUID, LabelTemplate, LabelReport) VALUES (");
            _ = sb.Append($"@LabelImageUID,");
            _ = sb.Append($"@LabelTemplate,");
            _ = sb.Append($"@LabelReport);");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
            {
                _ = command.Parameters.AddWithValue("LabelImageUID", imageUID);
                _ = command.Parameters.AddWithValue("LabelTemplate", template);
                _ = command.Parameters.AddWithValue("LabelReport", report);
                command.ExecuteNonQuery();
            }

            if (!IsConnectionPersistent)
                Close();
        }

        public List<string> GetAllTables()
        {
            List<string> lst = new List<string>();

            if (!Open()) return lst;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT name FROM sqlite_schema WHERE type='table';", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
                while (rdr.Read())
                    lst.Add(rdr.GetString(0));

            if (!IsConnectionPersistent)
                Close();

            return lst;
        }

        public List<Row> GetAllRows(string tableName)
        {
            List<Row> lst = new List<Row>();

            if (!Open()) return lst;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{tableName}'", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
                while (rdr.Read())
                    lst.Add(new Row(rdr));

            if (!IsConnectionPersistent)
                Close();

            return lst;
        }

        public Row GetRow(string tableName, string labelImageUID)
        {
            Row row = null;

            if (!Open()) return row;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{tableName}' WHERE LabelImageUID='{labelImageUID}'", Connection))

                try
                {
                    using (SQLiteDataReader rdr = command.ExecuteReader())
                        while (rdr.Read())
                            row = new Row(rdr);
                }
                catch(Exception ex)
                {

                }


            if (!IsConnectionPersistent)
                Close();

            return row;
        }

        public Row DeleteRow(string tableName, string labelImageUID)
        {
            Row row = null;

            if (!Open()) return row;
            using (SQLiteCommand command = new SQLiteCommand($"DELETE FROM '{tableName}' WHERE LabelImageUID='{labelImageUID}'", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader()) { };

            if (!IsConnectionPersistent)
                Close();

            return row;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Connection != null)
                    {
                        Connection.Dispose();
                        Connection = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ReplayDatabase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

