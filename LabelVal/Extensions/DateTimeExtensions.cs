using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Extensions;
public static class DateTimeExtensions
{
    public static int GetIntHashCode(this DateTime value)
    {
        long internalTicks = value.Ticks;
        return (((int)internalTicks) ^ ((int)(internalTicks >> 0x20)));
    }
}
