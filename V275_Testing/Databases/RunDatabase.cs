using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.Databases
{
    public class RunDatabase : IDisposable
    {
        public string FilePath { get; private set; }

        private SQLiteConnection Connection { get; set; } = null;
        public bool IsConnectionPersistent { get; set; }

        public class RunRow
        {
            public float EntryTime { get; set; }
            public int Repeat { get; set; }
            public string Job { get; set; }
            public string Report { get; set; }

            public RunRow(SQLiteDataReader rdr)
            {
                EntryTime = Convert.ToInt32(rdr["EntryTime"]);
                Repeat = Convert.ToInt32(rdr["Repeat"]);
                Job = rdr["Job"].ToString();
                Report = rdr["Report"].ToString();
            }
        }

        public RunDatabase(string filePath, bool isConnectionPersistent = true)
        {
            FilePath = filePath;
            IsConnectionPersistent = isConnectionPersistent;

            CreateFile();
        }

        private string SerializeObject(object o) => Newtonsoft.Json.JsonConvert.SerializeObject(o, o.GetType(), new Newtonsoft.Json.JsonSerializerSettings());


        private void CreateFile(bool overwrite = false)
        {
            if (overwrite && System.IO.File.Exists(FilePath))
                System.IO.File.Delete(FilePath);

            if (!System.IO.File.Exists(FilePath))
                SQLiteConnection.CreateFile(FilePath);
        }

        private bool Open()
        {
            if (Connection == null)
                Connection = new SQLiteConnection($"Data Source={FilePath}; Version=3;");

            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();

            if (Connection.State != System.Data.ConnectionState.Open)
                return false;
            else
                return true;
        }

        public void Close()
        {
            if (Connection == null)
                return;

            Connection.Close();
            Connection.Dispose();
            Connection = null;
        }

        public void CreateTables()
        {
            if (!Open()) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"CREATE TABLE 'Run'");
            sb.Append(" (");
            sb.Append("EntryTime REAL default current_timestamp");
            sb.Append(",");
            sb.Append("Repeat INTEGER"); 
            sb.Append(",");
            sb.Append("Job TEXT");
            sb.Append(",");
            sb.Append("Report TEXT");
            sb.Append(");");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
                command.ExecuteNonQuery();

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

        public void AddRunRow(RunRow row) => AddRunRow(row.Repeat, row.Job, row.Report);
        public void AddRunRow(int repeat, string job, string report)
        {
            if (!Open()) return;

            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"INSERT OR REPLACE INTO 'Run' (Repeat, Job, Report) VALUES ("); 
            _ = sb.Append($"@Repeat,");
            _ = sb.Append($"@Job,");
            _ = sb.Append($"@Report);");

            using (SQLiteCommand command = new SQLiteCommand(sb.ToString(), Connection))
            {
                _ = command.Parameters.AddWithValue("Repeat", repeat);
                _ = command.Parameters.AddWithValue("Job", job);
                _ = command.Parameters.AddWithValue("Report", report);
                command.ExecuteNonQuery();
            }

            if (!IsConnectionPersistent)
                Close();
        }

        public List<RunRow> GetAllRunRows()
        {
            List<RunRow> lst = new List<RunRow>();

            if (!Open()) return lst;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM 'Run'", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader())
                while (rdr.Read())
                    lst.Add(new RunRow(rdr));

            if (!IsConnectionPersistent)
                Close();

            return lst;
        }

        public RunRow GetRunRow(int repeat)
        {
            RunRow row = null;

            if (!Open()) return row;

            using (SQLiteCommand command = new SQLiteCommand($"SELECT * FROM 'Run' WHERE Repeat={repeat}", Connection))

                try
                {
                    using (SQLiteDataReader rdr = command.ExecuteReader())
                        while (rdr.Read())
                            row = new RunRow(rdr);
                }
                catch
                {

                }


            if (!IsConnectionPersistent)
                Close();

            return row;
        }

        public void DeleteRunRow(int repeat)
        {
            if (!Open()) return;
            using (SQLiteCommand command = new SQLiteCommand($"DELETE FROM 'Run' WHERE Repeat={repeat}", Connection))
            using (SQLiteDataReader rdr = command.ExecuteReader()) { };

            if (!IsConnectionPersistent)
                Close();
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

