using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Mapping;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using V725_REST_lib.Models;

namespace LabelVal.ClassMaps
{
    public class ReportMap : ClassMap<Report.Inspectlabel>
    {
        public ReportMap()
        {
            Id(x => x.id);

            Map(x => x.repeat);
            Map(x => x.voidRepeat);
            Map(x => x.iteration);
            Map(x => x.result);
            Map(x => x.width);
            Map(x => x.height);

            References(x => x.userAction).Cascade.SaveUpdate();

            //DynamicComponent(x => x.inspectSector,
            //             c =>
            //             {
                             
            //             });
            //HasManyToMany(x => x.inspectSector);

            //Map(x => x.ioLines);
        }
    }
}
