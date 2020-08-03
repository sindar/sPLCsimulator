using System;
using zModBusTCPServer;

namespace sPLCsimulator
{
    class Program
    {
        private static ushort[] HoldingRegs = new ushort[65536];
        private static ushort[] InputRegs = new ushort[65536];

        static void Main(string[] args)
        {
            //InitRegs();
            ModBusTCPServer MBTCPServer = new ModBusTCPServer(HoldingRegs, InputRegs);
            MBTCPServer.StartServer();
            Console.WriteLine("ModBusTCP server is started: " + DateTime.Now);
            for (;;)
            {
                ++HoldingRegs[0];
                --InputRegs[0];
                MBTCPServer.ProcessWriteQueue();
            }
        }

        static void InitRegs()
        {
            for (int i = 0; i < 65536; ++i)
                HoldingRegs[i] = (ushort)(i + 1);
        }
    }
}
