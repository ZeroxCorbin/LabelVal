using LabelVal.Extensions;
using System;

namespace LabelVal.Run.Databases;
public class QueueEntry
{
    public int ID { get; set; } = DateTime.Now.GetIntHashCode();


}
