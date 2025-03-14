using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using BarcodeVerification.lib.ISO;
using BarcodeVerification.lib.ISO.ParameterTypes;

namespace LabelVal.Sectors.Classes;

public class SectorElement(IParameterValue previous, IParameterValue current, AvailableSymbologies symbol)
{
    public Type DataType => Parameter.GetParameterDataType(Device, Symbol);
    public AvailableParameters Parameter => GetParameter();
    public AvailableDevices Device => GetDevice();
    public AvailableSymbologies Symbol => symbol;
    public IParameterValue Previous { get; } = previous;
    public IParameterValue Current { get; } = current;

    public bool Difference => Previous != null && Current != null && Previous.CompareTo(Current) != 0;

    private AvailableParameters GetParameter() => Current is IParameterValue currentParameter
            ? currentParameter.Parameter
            : Previous is IParameterValue previousParameter ? previousParameter.Parameter : AvailableParameters.Unknown;

    private AvailableDevices GetDevice() => Current is IParameterValue currentDeviceValue
            ? currentDeviceValue.Device
            : Previous is IParameterValue previousDeviceValue ? previousDeviceValue.Device : AvailableDevices.All;

}
