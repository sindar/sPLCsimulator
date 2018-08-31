using System;
namespace sPLCsimulator
{
    public enum ModBusFunctionCodes : byte
    {
        ReadHoldingRegs = 3,
        ReadInputRegs = 4,
        WriteMultipleHoldingRegs = 16
    }
}
