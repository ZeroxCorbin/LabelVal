using LabelVal.Core;
using LabelVal.Databases;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;

namespace LabelVal.ORM_Test
{
    public class NHibernateSettingsViewModel : Core.BaseViewModel
    {

        public ObservableCollection<string> SQLConfigurations { get; } = new ObservableCollection<string>();
        public string SQLConfiguration
        {
            get => App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "");
            set
            {
                App.Settings.SetValue("ORMTest_SelectedSQLConfiguration", value);
                OnPropertyChanged("Server");
                OnPropertyChanged("Port");
                OnPropertyChanged("Database");
                OnPropertyChanged("Username");
                OnPropertyChanged("Password");
            }
        }

        public string SQLStatement
        {
            get => App.Settings.GetValue("ORMTest_SQLStatement", "");
            set
            {
                App.Settings.SetValue("ORMTest_SQLStatement", value);
                OnPropertyChanged("SQLStatement");
            }
        }

        public string SQLResponse
        {
            get => App.Settings.GetValue("ORMTest_SQLResponse", "");
            set
            {
                App.Settings.SetValue("ORMTest_SQLResponse", value);
                OnPropertyChanged("SQLResponse");
            }
        }

        public bool FixJSON
        {
            get => App.Settings.GetValue("ORMTest_FixJSON", true);
            set
            {
                App.Settings.SetValue("ORMTest_FixJSON", value);
                OnPropertyChanged("FixJSON");
            }
        }

        public DataTable DataTable
        {
            get => App.Settings.GetValue<DataTable>("ORMTest_DataTable", null);
            set
            {
                App.Settings.SetValue("ORMTest_DataTable", value);
                OnPropertyChanged("DataTable");
            }
        }


        public string Server { get => App.Settings.GetValue($"ORMTest_ConnectionString_Server{SQLConfiguration}", ""); set { App.Settings.SetValue($"ORMTest_ConnectionString_Server{SQLConfiguration}", value); OnPropertyChanged("Server"); } }
        public uint Port { get => App.Settings.GetValue<uint>($"ORMTest_ConnectionString_Port{SQLConfiguration}", 0); set { App.Settings.SetValue($"ORMTest_ConnectionString_Port{SQLConfiguration}", value); OnPropertyChanged("Port"); } }
        public string Database { get => App.Settings.GetValue($"ORMTest_ConnectionString_Database{SQLConfiguration}", ""); set { App.Settings.SetValue($"ORMTest_ConnectionString_Database{SQLConfiguration}", value); OnPropertyChanged("Database"); } }
        public string Username { get => App.Settings.GetValue($"ORMTest_ConnectionString_Username{SQLConfiguration}", ""); set { App.Settings.SetValue($"ORMTest_ConnectionString_Username{SQLConfiguration}", value); OnPropertyChanged("Username"); } }
        public string Password { get => App.Settings.GetValue($"ORMTest_ConnectionString_Password{SQLConfiguration}", ""); set { App.Settings.SetValue($"ORMTest_ConnectionString_Password{SQLConfiguration}", value); OnPropertyChanged("Password"); } }

        public ICommand ExecuteStatement { get; }

        public NHibernateSettingsViewModel()
        {
            LoadSQLConfigurations();

            ExecuteStatement = new Core.RelayCommand(ExecuteStatementAction, c => true);
        }

        private void ExecuteStatementAction(object obj)
        {

            using (var db = new ORMTestingDatabase($"_ORMTesting.sqlite", false))
            {
                if (SQLStatement.StartsWith("select", StringComparison.CurrentCultureIgnoreCase))
                {

                    var dt = db.SelectDataTable(SQLStatement);

                    if(dt.Columns.Contains("REPEATIMAGE"))
                        foreach(DataRow row in dt.Rows)
                            row.SetField<string>("REPEATIMAGE", null);

                    DataTable = dt;
                    //SQLResponse = JsonConvert.SerializeObject(dt);
                    //if (string.IsNullOrEmpty(str))
                    //{
                    //    SQLResponse = "{}";
                    //    return;
                    //}

                    //if (SQLStatement.Contains("FROM Report"))
                    //{
                    //    var data = JsonConvert.DeserializeObject<List<Report>>(str);

                    //    foreach (var dat in data)
                    //    {
                    //        dat.repeatImage = null;
                    //    }

                    //    SQLResponse = JsonConvert.SerializeObject(data);
                    //}
                    //else
                    //{
                    //    SQLResponse = str;
                    //}

                }

            }
        }

        private void LoadSQLConfigurations()
        {
            SQLConfigurations.Clear();

            //        var types = Assembly
            //.GetExecutingAssembly()
            //.GetTypes()
            //.Where(t => t.Namespace.StartsWith("FluentNHibernate.Cfg.Db"));


            foreach (var type in GetTypesInNamespace(Assembly.GetAssembly(typeof(FluentNHibernate.Cfg.Db.DB2400Configuration)), "FluentNHibernate.Cfg.Db"))
            {
                if (type.Name.Contains("Configuration"))
                    SQLConfigurations.Add(type.Name);
            }
        }


        private Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            var types = assembly.GetTypes();
            return types
                      .Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                      .ToArray();
        }
    }
}
