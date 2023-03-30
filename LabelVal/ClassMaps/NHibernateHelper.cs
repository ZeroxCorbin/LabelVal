using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using Mono.Cecil;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Tool.hbm2ddl;
using V725_REST_lib.Models;
using FluentNHibernate.Automapping;

namespace LabelVal.ClassMaps
{
    internal class NHibernateHelper
    {
        private static ISessionFactory _sessionFactory;

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                    InitializeSessionFactory(); return _sessionFactory;
            }
        }

        private static void InitializeSessionFactory()
        {
            _sessionFactory = (ISessionFactory)Fluently.Configure()
            .Database(
                SQLiteConfiguration.Standard
                .UsingFile("NHibernateTesting.db")
            )
            .Mappings(m => m.AutoMappings.Add(
                AutoMap.AssemblyOf<V725_REST_lib.Models.Reports.Report>()
                .Where(t => t.Namespace == "V725_REST_lib.Models.Reports")))
            .ExposeConfiguration(cfg => new SchemaExport(cfg)
            .Create(true, true))
            .BuildSessionFactory();

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

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }

        public static void CloseSession() { _sessionFactory.Close(); }
    }
}
