using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace sPLCsimulator
{
    public class ModBusTCPServer
    {
        private const uint CLIENT_TIMEOUT = 5000;
        private const uint MAX_CONNECTIONS = 10;

        //private Dictionary<byte, Func<byte[], byte[]>> modBusFunctions;
        private Dictionary<ModBusFunctionCodes, Func<byte[], byte[]>> modBusFunctions;

        private ManualResetEvent queryDone = new ManualResetEvent(false);
        private Socket server = null;
        private UInt16[] HoldingRegs = new UInt16[65536];
        private Timer processingTimer;
        private uint connectionsCount = 0;

        private class ClientHandler
        {
            public const int BufferSize = 256;
            public Socket ClientConnection { get; set; }
            public Timer noActivityTimer;
            public bool timeOut = false;
            public int activityCount = 0;
            byte[] buffer = new byte[BufferSize];

            public byte[] Buffer
            {
                get { return buffer; }
                set { buffer = value; }
            }

            public void CloseConnection()
            {
                this.ClientConnection.Shutdown(SocketShutdown.Both);
                this.ClientConnection.Close();
            }
        }

        public ModBusTCPServer()
        {
            for (int i = 0; i < 65536; ++i)
                HoldingRegs[i] = (UInt16)(i + 1);
        }

        public bool StartServer()
        {
            if (server != null && server.Connected)
                server.Disconnect(false);

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                ProtocolType.Tcp);
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 502);

            try
            {
                server.Bind(endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error binding socket!" + ex.Message
                                  + DateTime.Now);
                return false;
            }

            try
            {
                server.Listen(20);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, cannot start listening!" + ex.Message
                                  + DateTime.Now);
                return false;
            }

            //modBusFunctions = new Dictionary<byte, Func<byte[], byte[]>>();
            modBusFunctions 
                = new Dictionary<ModBusFunctionCodes, Func<byte[], byte[]>>(); 
            modBusFunctions.Add(ModBusFunctionCodes.ReadHoldingRegs,
                                ReadHoldingRegs);

            processingTimer = new Timer(ProcessingTimerCallback,
                                        this,
                                        1000,
                                        1000);
            return true;
        }

        private void ProcessingTimerCallback(object state)
        {
            ModBusTCPServer thisServer = (ModBusTCPServer)state;
            if (thisServer.connectionsCount < MAX_CONNECTIONS)
            {
                AcceptConnections();
                ++connectionsCount;
            }
        }

        private void AcceptConnections()
        {
            server.BeginAccept(new AsyncCallback(AsyncAcceptCallback), server);
        }

        private void AsyncAcceptCallback(IAsyncResult ar)
        {
            Socket serverSocket = (Socket)ar.AsyncState;

            ClientHandler clientHandler = new ClientHandler();
            clientHandler.ClientConnection = serverSocket.EndAccept(ar);
            Console.WriteLine("Client connected: " + DateTime.Now);

            clientHandler.timeOut = false;
            clientHandler.noActivityTimer = new Timer(NoActivityTimerCallback,
                                                      clientHandler,
                                                      CLIENT_TIMEOUT,
                                                      CLIENT_TIMEOUT);

            queryDone.Reset();
            clientHandler.ClientConnection.BeginReceive(clientHandler.Buffer, 0,
                                                     ClientHandler.BufferSize,
                                                     SocketFlags.None,
                                                     new AsyncCallback(ReceiveCallback),
                                                     clientHandler);
        }

        void NoActivityTimerCallback(object state)
        {
            ClientHandler clientHandler = (ClientHandler)state;
            if (--clientHandler.activityCount < 0)
            {
                clientHandler.timeOut = true;
                queryDone.Set();
                Console.WriteLine("No activity from client, closing socket: "
                                   + DateTime.Now);
                --connectionsCount;
                clientHandler.CloseConnection();
                clientHandler.noActivityTimer.Dispose();
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientHandler clientHandler = (ClientHandler)ar.AsyncState;
            int bytes;

            try
            {
                bytes = clientHandler.ClientConnection.EndReceive(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got excpetion:" + ex.Message);
                return;
            }

            if (bytes > 0)
            {
                clientHandler.activityCount = 1;
                byte[] receivedData = clientHandler.Buffer;
                byte[] transmitData;
                ModBusFunctionCodes funcCode 
                    = (ModBusFunctionCodes)receivedData[7];

                if (modBusFunctions.ContainsKey(funcCode))
                {
                    transmitData = modBusFunctions[funcCode](receivedData);
                }
                else
                {
                    transmitData = ExceptionResponse(receivedData,
                                                     ModBusExceptionCodes.IllegalFunction);
                }

                clientHandler.ClientConnection.BeginSend(transmitData, 0,
                                                      transmitData.Length,
                                                      SocketFlags.None,
                                                      new AsyncCallback(SendCallback),
                                                      clientHandler);

                queryDone.WaitOne();
                if (!clientHandler.timeOut)
                {
                    clientHandler.ClientConnection.BeginReceive(clientHandler.Buffer, 0,
                                                         ClientHandler.BufferSize,
                                                         SocketFlags.None,
                                                         new AsyncCallback(ReceiveCallback),
                                                         clientHandler);
                }
                else
                {
                    Console.WriteLine("Timeout: " + DateTime.Now);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                ClientHandler clientHandler = (ClientHandler)ar.AsyncState;
                int bytesSent = clientHandler.ClientConnection.EndSend(ar);

                ++HoldingRegs[0];
                queryDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #region ModBus Functions

        private byte[] ExceptionResponse(byte[] receivedData, 
                                         ModBusExceptionCodes exCode)
        {
            byte[] transmitData = new byte[9];
            //======Preparing Header=========
            for (int i = 0; i < 4; ++i)
                transmitData[i] = receivedData[i];

            transmitData[4] = 0;
            transmitData[5] = 3;

            transmitData[6] = receivedData[6]; //Unit ID
            transmitData[7] = (byte)(receivedData[7] & 0x80); //Function Code
            //======Preparing Header=========

            transmitData[8] = (byte)exCode; //Illegal Function Exception Code

            return transmitData;
        }

        private byte[] ReadHoldingRegs(byte[] receivedData)
        {
            UInt16 dataLength = (UInt16)((UInt16)(receivedData[10] << 8)
                                              | (UInt16)receivedData[11]);
            UInt16 firstregister = (UInt16)((UInt16)(receivedData[8] << 8)
                                             | (UInt16)receivedData[9]);
            byte[] transmitData = new byte[9 + dataLength * 2];
            UInt16 remainBytes = (UInt16)(3 + dataLength * 2);

            //======Preparing Header=========
            for (int i = 0; i < 4; ++i)
                transmitData[i] = receivedData[i];

            transmitData[4] = (byte)(remainBytes >> 8);
            transmitData[5] = (byte)remainBytes;

            transmitData[6] = receivedData[6]; //Unit ID
            transmitData[7] = receivedData[7]; //Function Code
                                               //======Preparing Header=========

            transmitData[8] = (byte)(dataLength * 2); //Bytes count

            //======Data======
            for (int i = firstregister, j = 0; i < firstregister + dataLength; ++i)
            {
                transmitData[9 + j++] = (byte)(HoldingRegs[i] >> 8);
                transmitData[9 + j++] = (byte)HoldingRegs[i];
            }
            //======Data======

            return transmitData;
        }
        #endregion

    }
}
