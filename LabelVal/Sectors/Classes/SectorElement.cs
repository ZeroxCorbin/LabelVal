using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO.ParameterTypes;

namespace LabelVal.Sectors.Classes;

public class SectorElement(IParameterValue previous, IParameterValue current, ICompareSettings settings, Symbologies symbol)
{
    public Type DataType => Parameter.GetDataType(Device, Symbol);
    public Parameters Parameter => GetParameter();
    public Devices Device => GetDevice();
    public Symbologies Symbol => symbol;
    public IParameterValue Previous { get; } = previous;
    public IParameterValue Current { get; } = current;
    public ICompareSettings Settings { get; } = settings;

    public bool Difference => Previous != null && Current != null && Previous.CompareTo(Current, Settings) != 0;

    private Parameters GetParameter() => Current is IParameterValue currentParameter
            ? currentParameter.Parameter
            : Previous is IParameterValue previousParameter ? previousParameter.Parameter : Parameters.Unknown;

    private Devices GetDevice() => Current is IParameterValue currentDeviceValue
            ? currentDeviceValue.Device
            : Previous is IParameterValue previousDeviceValue ? previousDeviceValue.Device : Devices.All;

}
