using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V725_REST_lib.Models;

namespace LabelVal.ClassMaps
{
    public class UserActionMap : ClassMap<Report.Useraction>
    {

        public UserActionMap()
        {
            Id(x => x.id);

            Map(x => x.action);
            Map(x => x.user);
            Map(x => x.note);
        }
    }
}
