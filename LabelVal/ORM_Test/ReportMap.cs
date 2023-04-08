using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace LabelVal.ORM_Test
{
    public class ReportMap : ClassMap<Report>
    {
        public ReportMap()
        {
            Id(x => x.id);

            //MySQL considers 'repeat' a reserved word. It can not be used for a column name.
            Map(x => x.repeat, "repeatNumber");
            Map(x => x.voidRepeat, "voidRepeatNumber");
            Map(x => x.iteration);
            Map(x => x.result);
            Map(x => x.width);
            Map(x => x.height);

            //Length() is required to store the entire JSON string.
            //Postgres defaults to [256] length strings
            Map(x => x.userAction).Length(4096);

            Map(x => x.inspectSector).Length(500000);

            Map(x => x.ioLines).Length(4096);

            //Length() is required to store the image.
            Map(x => x.repeatImage).Length(Int32.MaxValue);
        }





    }
}
