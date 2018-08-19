using System;

namespace sPLCsimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            ModBusTCPServer MBTCPServer = new ModBusTCPServer();
            MBTCPServer.StartServer();
            for (; ; );
        }
    }
}
