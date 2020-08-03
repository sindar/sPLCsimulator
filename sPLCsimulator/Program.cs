using System;
using System.Diagnostics;
using zModBusTCPServer;

namespace sPLCsimulator
{
    class Program
    {
        private static ushort[] HoldingRegs = new ushort[65536];
        private static ushort[] InputRegs = new ushort[65536];

        static void Main(string[] args)
        {
            ModBusTCPServer MBTCPServer = new ModBusTCPServer(HoldingRegs, InputRegs);
            MBTCPServer.StartServer();
            Console.WriteLine("ModBusTCP server is started: " + DateTime.Now);

            if(args.Length > 0)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nohup",
                        Arguments = args[0],
                        UseShellExecute = true,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                Console.WriteLine("File {0} is started", args[0]);
            }

            for (;;)
            {
                ++HoldingRegs[0];
                --InputRegs[0];
                MBTCPServer.ProcessWriteQueue();
            }
        }
    }
}
