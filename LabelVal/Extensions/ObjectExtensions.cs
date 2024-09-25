using System;
using System.Numerics;

namespace LabelVal.Extensions;
public static class ObjectExtensions
{
    public static bool IsNumber(this object value) => Array.Exists(value.GetType().GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INumber<>));
    public static double ToDouble(this object value) => Convert.ToDouble(value);
}
