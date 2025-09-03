using System;
using Wpf.lib.Extentions;

namespace LabelVal.Run.Databases;
public class QueueEntry
{
    public int ID { get; set; } = DateTime.Now.GetIntHashCode();

}
