using System;
namespace ModBusTCPServerLib
{
    public enum ModBusExceptionCodes : byte
    {
        IllegalFunction = 1,
        IllegalDataAddress,
        IllegalDataValue,
        SlaveDeviceFailure,
        Acknowledge,
        SlaveDeviceBusy,
        NegativeAcknowledge,
        MemoryParityError,
        GatewayPathUnavailable,
        GatewayTargetDeviceFailedToRespond
    }
}
