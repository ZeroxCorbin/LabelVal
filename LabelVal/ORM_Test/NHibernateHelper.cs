using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Tool.hbm2ddl;
using V275_REST_Lib.Models;
using FluentNHibernate.Automapping;

namespace LabelVal.ORM_Test
{
    internal class NHibernateHelper
    {
        private ISessionFactory _sessionFactory;

        private ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                    InitializeSessionFactory();
                
                return _sessionFactory;
            }
        }

        private void InitializeSessionFactory()
        {
            if (App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "").Equals("SQLiteConfiguration"))
            {
                try
                {
                    _sessionFactory = (ISessionFactory)Fluently.Configure()
                    .Database(
                        SQLiteConfiguration.Standard.UsingFile($"{App.Settings.GetValue($"ORMTest_ConnectionString_Database{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", "")}.sqlite")
                    )
                    .Mappings(m =>
                    {
                        m.FluentMappings.Add<LabelVal.ORM_Test.ReportMap>();
                        m.FluentMappings.Add<LabelVal.ORM_Test.RunLedgerMap>();
                    })
                    //.Where(t => t.Namespace == "LabelVal.ORM_Test.ClassMaps")))
                    .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                    .BuildSessionFactory();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            if (App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "").Equals("MySQLConfiguration"))
            {
                var connection = MySQLConfiguration.Standard
                    .ConnectionString(c => c
                        .Server(App.Settings.GetValue($"ORMTest_ConnectionString_Server{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Database(App.Settings.GetValue($"ORMTest_ConnectionString_Database{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Username(App.Settings.GetValue($"ORMTest_ConnectionString_Username{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Password(App.Settings.GetValue($"ORMTest_ConnectionString_Password{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Port(App.Settings.GetValue($"ORMTest_ConnectionString_Port{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", 0))
                    )
                    .ShowSql().FormatSql();

                try
                {
                    _sessionFactory = (ISessionFactory)Fluently.Configure()
                    .Database(connection)
                    .Mappings(m =>
                    {
                        m.FluentMappings.Add<LabelVal.ORM_Test.ReportMap>();
                        m.FluentMappings.Add<LabelVal.ORM_Test.RunLedgerMap>();
                    })
                    .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                    .BuildSessionFactory();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }


            if (App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "").Equals("PostgreSQLConfiguration"))
            {
                var connection = PostgreSQLConfiguration.Standard
                    .ConnectionString(c => c
                        .Host(App.Settings.GetValue($"ORMTest_ConnectionString_Server{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Database(App.Settings.GetValue($"ORMTest_ConnectionString_Database{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Username(App.Settings.GetValue($"ORMTest_ConnectionString_Username{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Password(App.Settings.GetValue($"ORMTest_ConnectionString_Password{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Port(App.Settings.GetValue($"ORMTest_ConnectionString_Port{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", 0))
                    )
                    .ShowSql().FormatSql();

                try
                {
                    _sessionFactory = (ISessionFactory)Fluently.Configure()
                    .Database(connection)
                    .Mappings(m =>
                    {
                        m.FluentMappings.Add<LabelVal.ORM_Test.ReportMap>();
                        m.FluentMappings.Add<LabelVal.ORM_Test.RunLedgerMap>();
                    })
                    .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                    .BuildSessionFactory();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            if (App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "").Equals("MsSqlConfiguration"))
            {
                var connection = MsSqlConfiguration.MsSql7
                    .ConnectionString(c => c
                        .Server(App.Settings.GetValue($"ORMTest_ConnectionString_Server{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Database(App.Settings.GetValue($"ORMTest_ConnectionString_Database{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Username(App.Settings.GetValue($"ORMTest_ConnectionString_Username{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Password(App.Settings.GetValue($"ORMTest_ConnectionString_Password{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                    )
                    .ShowSql().FormatSql();

                try
                {
                    _sessionFactory = (ISessionFactory)Fluently.Configure()
                    .Database(connection)
                    .Mappings(m =>
                    {
                        m.FluentMappings.Add<LabelVal.ORM_Test.ReportMap>();
                        m.FluentMappings.Add<LabelVal.ORM_Test.RunLedgerMap>();
                    })
                    .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                    .BuildSessionFactory();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            if (App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "").Equals("OracleManagedDataClientConfiguration"))
            {
                var connection = OracleManagedDataClientConfiguration.Oracle10
                    .ConnectionString(c => c
                        .Server(App.Settings.GetValue($"ORMTest_ConnectionString_Server{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Instance(App.Settings.GetValue($"ORMTest_ConnectionString_Database{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Username(App.Settings.GetValue($"ORMTest_ConnectionString_Username{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                        .Password(App.Settings.GetValue($"ORMTest_ConnectionString_Password{App.Settings.GetValue("ORMTest_SelectedSQLConfiguration", "")}", ""))
                    ).Dialect("NHibernate.Dialect.Oracle12cDialect")
                    .ShowSql().FormatSql();

                try
                {
                    _sessionFactory = (ISessionFactory)Fluently.Configure()
                    .Database(connection)
                    .Mappings(m =>
                    {
                        m.FluentMappings.Add<LabelVal.ORM_Test.ReportMap>();
                        m.FluentMappings.Add<LabelVal.ORM_Test.RunLedgerMap>();
                    })
                    .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                    .BuildSessionFactory();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            //string Data_Source = asia13797\\sqlexpress;
            //   String Initial_Catalog = NHibernateDemoDB;
            //   String Integrated_Security = True;
            //   String Connect_Timeout = 15;
            //   String Encrypt = False;
            //   String TrustServerCertificate = False;
            //   String ApplicationIntent = ReadWrite;
            //   String MultiSubnetFailover = False;

            //.Database(MsSqlConfiguration.MsSql2008.ConnectionString(
            //   @"Data Source + Initial Catalog + Integrated Security + Connect Timeout
            //   + Encrypt + TrustServerCertificate + ApplicationIntent + 
            //   MultiSubnetFailover").ShowSql())

            //.Mappings(m => m.FluentMappings
            //.AddFromAssemblyOf<App>())
            //.ExposeConfiguration(cfg => new SchemaExport(cfg)
            //.Create(true, true))
            //.BuildSessionFactory();
        }

        public ISession OpenSession()
        {
            return SessionFactory?.OpenSession();
        }

        public void CloseSession() { _sessionFactory.Close(); }
    }
}
