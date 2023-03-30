using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V725_REST_lib.Models;

namespace LabelVal.ClassMaps
{
    public class Report_InspectSector_Verify1D_Map : ClassMap<Report_InspectSector_Verify1D>
    {

        public Report_InspectSector_Verify1D_Map()
        {
            Id(x => x.id);

            Map(x => x.report_id);

            Map(x => x.name);
            Map(x => x.type);
            Map(x => x.top);
            Map(x => x.left);
            Map(x => x.width);
            Map(x => x.height);

            HasMany<Report_InspectSector_Verify1D.Data>(x => x.data);

        }
    }
}
