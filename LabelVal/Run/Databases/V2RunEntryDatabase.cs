
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Mozilla;
using SQLite;
using System;
using System.Collections.Generic;

namespace LabelVal.Run.Databases
{
    public class RunEntryDatabase : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public class RunEntry : Core.BaseViewModel
        {

            [PrimaryKey]
            public int cycleID { get; set; }
            public bool cyclePassed { get; set; }
            public string imageURL { get; set; }
            public string reportData { get; set; }
            public string timeStamp { get; set; }
            public int voidID { get; set; }


        }

        private SQLiteConnection Connection { get; set; } = null;

        public RunEntryDatabase Open(string dbFilePath)
        {
            Logger.Info("Opening Database: {file}", dbFilePath);

            if (string.IsNullOrEmpty(dbFilePath))
                return null;

            try
            {
                if (Connection == null)
                    Connection = new SQLiteConnection(dbFilePath);

                Connection.CreateTable<RunEntry>();

                return this;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }
        public void Close() => Connection?.Dispose();

        public int InsertOrReplace(RunEntry entry) => Connection.InsertOrReplace(entry);

        public bool ExistsRunEntry(int cycleID) => Connection.Table<RunEntry>().Where(v => v.cycleID == cycleID).Count() > 0;
        public RunEntry SelectRunEntry(int cycleID) => Connection.Table<RunEntry>().Where(v => v.cycleID == cycleID).FirstOrDefault();
        public int DeleteRunEntry(int cycleID) => Connection.Table<RunEntry>().Delete(v => v.cycleID == cycleID);

        public List<RunEntry> SelectAllRunEntries() => Connection.CreateCommand("select * from reportData").ExecuteQuery<RunEntry>();



        public void Dispose()
        {
            Connection?.Close();
            Connection?.Dispose();
        }
    }
}
