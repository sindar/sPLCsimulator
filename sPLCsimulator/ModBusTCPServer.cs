using System;
using System.Net;
using System.Net.Sockets;

namespace sPLCsimulator
{
    public class ModBusTCPServer
    {
        private Socket server = null;
        private UInt16[] HoldingRegs = new UInt16[65536];

        private class SocketData
        {
            public const int BufferSize = 256;

            public Socket ClientConnection { get; set; }

            byte[] buffer = new byte[BufferSize];

            public byte[] Buffer
            {
                get { return buffer; }
                set { buffer = value; }
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
                // TODO: Log start listening error
                Console.WriteLine("Error binding socket!" + ex.Message);
                return false;
            }

            try
            {
                server.Listen(10);
            }
            catch (Exception ex)
            {
                // TODO: Log start listening error
                Console.WriteLine("Error, cannot start listening!" + ex.Message);
                return false;
            }

            return true;
        }

        public void AcceptConnections()
        {
            server.BeginAccept(new AsyncCallback(AsyncAcceptCallback), server);
        }

        private void AsyncAcceptCallback(IAsyncResult result)
        {
            Socket serverSocket = (Socket)result.AsyncState;

            SocketData socketData = new SocketData();
            socketData.ClientConnection = serverSocket.EndAccept(result);

            Console.WriteLine("Client connected...");
            socketData.ClientConnection.BeginReceive(socketData.Buffer, 0, 
                                                     SocketData.BufferSize,
                                                     SocketFlags.None, 
                                                     new AsyncCallback(ReadCallback), 
                                                     socketData);
        }

        private void ReadCallback(IAsyncResult result)
        {
            SocketData socketData = (SocketData)result.AsyncState;
            int bytes = socketData.ClientConnection.EndReceive(result);
            byte[] receivedData = socketData.Buffer;

            Console.WriteLine("Received request...");

            if (bytes > 0)
            {
                // TODO response
                //ответ-заглушка клиенту
                //byte[] reply = new byte[1] {1};
                //data.ClientConnection.Send(reply);

                UInt16 dataLength = (UInt16)((UInt16)(receivedData[10] << 8) 
                                              | (UInt16)receivedData[11]);
                UInt16 firstregister = (UInt16)((UInt16)(receivedData[8] << 8) 
                                                 | (UInt16)receivedData[9]);
                Byte[] transmitData = new Byte[9 + dataLength * 2];
                UInt16 remainBytes = (UInt16)(3 + dataLength * 2);

                //======Preparing Header=========
                for (int i = 0; i < 4; ++i)
                    transmitData[i] = receivedData[i];

                transmitData[4] = (Byte)(remainBytes >> 8);
                transmitData[5] = (Byte)remainBytes;

                transmitData[6] = receivedData[6]; //Unit ID
                transmitData[7] = receivedData[7]; //Function Code
                //======Preparing Header=========

                transmitData[8] = (Byte)(dataLength * 2); //Bytes count

                //======Data======
                for (int i = firstregister, j = 0; i < firstregister + dataLength; ++i)
                {
                    transmitData[9 + j++] = (Byte)(HoldingRegs[i] >> 8);
                    transmitData[9 + j++] = (Byte)HoldingRegs[i];
                }
                //======Data======

                socketData.ClientConnection.Send(transmitData);
            }

            ++HoldingRegs[0];
            socketData.ClientConnection.Disconnect(false);
            server.BeginAccept(new AsyncCallback(AsyncAcceptCallback), server);
        }
    }
}
