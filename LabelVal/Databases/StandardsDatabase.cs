using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Databases
{
    public class StandardsDatabase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class Row : Core.BaseViewModel
        {
            private byte[] labelImage;
            public byte[] LabelImage { get => labelImage; set => SetProperty(ref labelImage, value); }

            private string labelImageUID;
            public string LabelImageUID { get => labelImageUID; set => SetProperty(ref labelImageUID, value); }

            private string labelTemplate;
            public string LabelTemplate { get => labelTemplate; set => SetProperty(ref labelTemplate, value); }

            private string labelReport;
            public string LabelReport { get => labelReport; set => SetProperty(ref labelReport, value); }

            private byte[] repeatImage;
            public byte[] RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

            public Row(SQLiteDataReader rdr)
            {
                for (int i = 0; i < rdr.FieldCount; i++)
                {
                    if (rdr.GetName(i).Equals("LabelImage", StringComparison.InvariantCultureIgnoreCase))
                        LabelImage = (byte[])rdr["LabelImage"];

                }

                LabelImageUID = rdr["LabelImageUID"].ToString();
                LabelTemplate = rdr["LabelTemplate"].ToString();
                LabelReport = rdr["LabelReport"].ToString();
                RepeatImage = (byte[])rdr["RepeatImage"];
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
            sb.Append($"CREATE TABLE IF NOT EXISTS '{tableName}'");
            sb.Append(" (");
            sb.Append("LabelImageUID TEXT");
            sb.Append(",");
            sb.Append("LabelImage BLOB");
            sb.Append(",");
            sb.Append("LabelTemplate TEXT");
            sb.Append(",");
            sb.Append("LabelReport TEXT");
            sb.Append(",");
            sb.Append("RepeatImage BLOB");
            sb.Append(");");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
                command.ExecuteNonQuery();

            if (!IsConnectionPersistent)
                Close();
        }
        public void CreateLockTable(bool isPerminent)
        {
            string tableName = isPerminent ? "LOCKPERM" : "LOCK";

            using (SQLiteCommand command = new SQLiteCommand($"CREATE TABLE IF NOT EXISTS '{tableName}' ({tableName} TEXT);", Connection))
                command.ExecuteNonQuery();
        }
        public void DeleteLockTable(bool isPerminent)
        {
            string tableName = isPerminent ? "LOCKPERM" : "LOCK";

            using (SQLiteCommand command = new SQLiteCommand($"DROP TABLE IF EXISTS '{tableName}';", Connection))
                command.ExecuteNonQuery();
        }

        public void AddRow(string tableName, Row row) => AddRow(tableName, row.LabelImageUID, row.LabelImage, row.LabelTemplate, row.LabelReport, row.RepeatImage);
        public void AddRow(string tableName, string imageUID, byte[] labelImage, string template, string report, byte[] repeatImage)
        {
            if (!Open()) return;

            CreateTable(tableName);

            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"INSERT OR REPLACE INTO '{tableName}' (LabelImageUID, LabelImage, LabelTemplate, LabelReport, RepeatImage) VALUES (");
            _ = sb.Append($"@LabelImageUID,");
            _ = sb.Append($"@LabelImage,");
            _ = sb.Append($"@LabelTemplate,");
            _ = sb.Append($"@LabelReport,");
            _ = sb.Append($"@RepeatImage);");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
            {
                _ = command.Parameters.AddWithValue("LabelImageUID", imageUID);
                _ = command.Parameters.AddWithValue("LabelImage", labelImage);
                _ = command.Parameters.AddWithValue("LabelTemplate", template);
                _ = command.Parameters.AddWithValue("LabelReport", report);
                _ = command.Parameters.AddWithValue("RepeatImage", repeatImage);
                command.ExecuteNonQuery();
            }

            if (!IsConnectionPersistent)
                Close();
        }
        public Row GetRow(string tableName, string labelImageUID)
        {
            Row row = null;

            if (!Open()) return row;
            if (!TableExists(tableName))
                return row;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM '{tableName}' WHERE LabelImageUID='{labelImageUID}'", Connection))

                try
                {
                    using (SQLiteDataReader rdr = command.ExecuteReader())
                        while (rdr.Read())
                            row = new Row(rdr);
                }
                catch (Exception ex)
                {

                }


            if (!IsConnectionPersistent)
                Close();

            return row;
        }
        public void DeleteRow(string tableName, string labelImageUID)
        {
            if (!Open()) return;
            if (!TableExists(tableName)) return;

            using (SQLiteCommand command = new SQLiteCommand($"DELETE FROM '{tableName}' WHERE LabelImageUID='{labelImageUID}'", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader()) { };

            if (!IsConnectionPersistent)
                Close();
        }

        public bool TableExists(string tableName)
        {
            using (SQLiteCommand command = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';", Connection))
            {
                try
                {
                    using (SQLiteDataReader rdr = command.ExecuteReader())
                    {
                        if (rdr.HasRows)
                            return true;
                        else
                            return false;
                    }
                }
                catch (Exception ex)
                {

                }
            }

            if (!IsConnectionPersistent)
                Close();

            return false;
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

        public int GetAllRowsCount(string tableName)
        {
            int count = 0;
            if (!Open()) return count;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT COUNT(*) FROM '{tableName}'", Connection))
                count = Convert.ToInt32(command.ExecuteScalar());

            if (!IsConnectionPersistent)
                Close();

            return count;
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

