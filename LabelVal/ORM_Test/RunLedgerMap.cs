using FluentNHibernate.Mapping;
using FluentNHibernate.Testing.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.ORM_Test
{
    public class RunLedgerMap : ClassMap<RunLedger>
    {

        public RunLedgerMap()
        {
            Id(x => x.Id);

            Map(x => x.CreatedOn);
            Map(x => x.ComputerId);
            Map(x => x.SerialNumber);
            Map(x => x.Mac);
            Map(x => x.Job);

        }


    }
}
