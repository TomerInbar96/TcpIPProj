using SimpleTCP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class TcpClass
    {
        /// <summary>
        /// Stop the server service
        /// </summary>
        /// <param name="server"></param>
        public static void StopService(SimpleTcpServer server)
        {
            if (server.IsStarted)
            {
                server.Stop();
            }
        }

        public static void deleteFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        /// <summary>
        /// Send File From client to the server
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        public static void ClientSendFile(string FileName, string Host, int Port)
        {
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                client = new TcpClient(Host, Port);

                netstream = client.GetStream();
                FileStream Fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(1024)));
                int TotalLength = (int)Fs.Length, CurrentPacketLength, counter = 0;
                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (TotalLength > 1024)
                    {
                        CurrentPacketLength = 1024;
                        TotalLength = TotalLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = TotalLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                }
                Fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();
            }
        }

        /// <summary>
        /// The server recieve file from client
        /// </summary>
        /// <param name="Port"></param>
        /// <param name="FileName"></param>
        public static void ServerReceiveFile(int Port, string FileName)
        {
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            byte[] RecData = new byte[1024];
            int RecBytes;

            for (; ; )
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                try
                {

                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();

                        if (FileName != string.Empty)
                        {
                            int totalrecbytes = 0;

                            FileStream Fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                            while ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0)
                            {
                                Fs.Write(RecData, 0, RecBytes);
                                totalrecbytes += RecBytes;
                            }
                            Fs.Close();
                        }
                        netstream.Close();
                        client.Close();

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// The client send request for file to the server and handle it.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        public static void AskFileFromServer(string FileName, string Host, int Port)
        {
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                client = new TcpClient(Host, Port);

                netstream = client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(FileName);
                ServerClientMessage myMessage = new ServerClientMessage(MessageType.AskForFile, data.Length, data);

                data = myMessage.serialize();

                netstream.Write(data, 0, data.Length);
                
                HandleClientRequest(netstream, client, FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();
            }
        }

        /// <summary>
        /// The server listen to clients request for files and reply
        /// </summary>
        /// <param name="Port"></param>
        /// <param name="FileName"></param>
        public static void HandleFileRequest(int Port, string FileName)
        {
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            for (; ; )
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                try
                {

                    if (Listener.Pending())
                    {
                        //SendFileBack(Listener, client, netstream, FileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Send the file to the client
        /// </summary>
        /// <param name="Listener"></param>
        /// <param name="client"></param>
        /// <param name="netStream"></param>
        /// <param name="FileName"></param>
        private static void SendFileBack(NetworkStream netStream, ServerClientMessage myMessage)
        {
            byte[] SendingBuffer = null;
            try
            {
                byte[] dataSend;
                List<byte> byteList = new List<byte>();
                string FileName = Encoding.ASCII.GetString(myMessage.MyData);
                FileStream Fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(1024)));
                int FileLength = (int)Fs.Length, CurrentPacketLength;
                ServerClientMessage myReply = new ServerClientMessage(MessageType.DownloadAndExe, FileLength);

                // Run on the file and copy it to the messageReply
                // Todo: this is example for handling large requests, maybe we'll aplly later on the netStream writing
                for (int i = 0; i < NoOfPackets; i++)
                {
                    if (FileLength > 1024)
                    {
                        CurrentPacketLength = 1024;
                        FileLength = FileLength - CurrentPacketLength;
                    }
                    else
                        CurrentPacketLength = FileLength;
                    SendingBuffer = new byte[CurrentPacketLength];
                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
                    byteList.AddRange(SendingBuffer);
                }

                Fs.Close();

                myMessage.MyData = byteList.ToArray();
                dataSend = myMessage.serialize();
                netStream.Write(dataSend, 0, dataSend.Length);
                netStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
 

        /// <summary>
        /// The server listen to clients request for files and reply
        /// </summary>
        /// <param name="Port"></param>
        /// <param name="FileName"></param>
        public static void HandleServerRequest(int Port)
        {
            ServerClientMessage recivedMessage;
            TcpListener Listener = null;
            try
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            for (; ; )
            {
                TcpClient client = null;
                NetworkStream netstream = null;
                try
                {

                    if (Listener.Pending())
                    {
                        client = Listener.AcceptTcpClient();
                        netstream = client.GetStream();
                        recivedMessage  = GetMessageData(netstream);

                        switch (recivedMessage.MyMessageType)
                        {
                            case MessageType.AskForFile:
                                {
                                    SendFileBack(netstream, recivedMessage);
                                    break;
                                }
                            case MessageType.DownloadAndExe:
                                {
                                    break;
                                }
                            case MessageType.DownloadAndExeRes:
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Handling requests from the clients
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        public static void HandleClientRequest(NetworkStream netstream, TcpClient client, string Value)
        {
            try
            {
                ServerClientMessage recivedMessage;

                recivedMessage = GetMessageData(netstream);

                switch (recivedMessage.MyMessageType)
                {
                    case MessageType.DownloadAndExe:
                        {
                            DownloadAndExeFile(recivedMessage, Value);
                            break;
                        }

                    default:
                        {
                            Console.WriteLine("Invalid message type");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                netstream.Close();
                client.Close();
            }
        }

        private static void DownloadAndExeFile(ServerClientMessage MyMessage, string FileName)
        {
            if (FileName != string.Empty)
            {
                FileStream Fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);

                Fs.Write(MyMessage.MyData, 0, MyMessage.Size);
                Fs.Close();
                System.Diagnostics.Process.Start(FileName);
            }
        }
        
        private static ServerClientMessage GetMessageData(NetworkStream netStream)
        {
            List<byte> bytelist = new List<byte>();
            int Datasize = 0;
            ServerClientMessage MyMessage = new ServerClientMessage();
            byte[] RecData = new byte[4];
            byte[] data;
            int RecBytes;
            int totalrecbytes = 0;

            // Wait until data is available
            while (!netStream.DataAvailable)
            {
                Thread.Sleep(100);
            }
            
            // Read the first 4 bytes that declare the type of the message
            netStream.Read(RecData, 0, 4);

            // Add them to the data of the message
            bytelist.AddRange(RecData);

            // Wait until data is available
            while (!netStream.DataAvailable)
            {
                Thread.Sleep(100);
            }

            // Read the size of the data
            netStream.Read(RecData, 0, 4);
            Datasize = BitConverter.ToInt32(RecData, 0);
            bytelist.AddRange(RecData);
            RecData = new byte[Datasize];
            
            // Run loop Until all data arrived
            while (totalrecbytes < Datasize)
            {
                while (netStream.DataAvailable && ((RecBytes = netStream.Read(RecData, 0, RecData.Length)) > 0))
                {
                    data = new byte[RecBytes];
                    Array.Copy((Array)RecData, 0, data, 0, RecBytes);
                    bytelist.AddRange(data);
                    totalrecbytes += RecBytes;
                }

                // bring timeout for the next data to come
                if (totalrecbytes != Datasize)
                {
                    Thread.Sleep(100);
                }
            }

            MyMessage.DeSerialize(bytelist.ToArray());

            return (MyMessage);
        }
    }
}
