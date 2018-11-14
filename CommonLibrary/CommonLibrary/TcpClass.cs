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
        /// The client send request for file to the server and handle it.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        public static void AskFileFromServer(string FileName, string Host, int Port)
        {
            byte[] SendingBuffer = null;
            TcpClient client = null;
            NetworkStream netstream = null;
            try
            {
                client = new TcpClient(Host, Port);

                netstream = client.GetStream();
                int TotalLength = 1024;
                SendingBuffer = new byte[TotalLength];

                netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                netstream.Flush();

                while (!netstream.DataAvailable)
                {
                    Thread.Sleep(100);
                }
                
                    byte[] RecData = new byte[1024];
                    int RecBytes;

                    if (FileName != string.Empty)
                    {
                        int totalrecbytes = 0;

                        FileStream Fss = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                        while (netstream.DataAvailable && ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0))
                        {
                            Fss.Write(RecData, 0, RecBytes);
                            totalrecbytes += RecBytes;
                        }
                        Fss.Close();
                        System.Diagnostics.Process.Start(FileName);
                        netstream.Close();
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
                        SendFileBack(Listener, client, netstream, FileName);
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
        private static void SendFileBack(TcpListener Listener, TcpClient client, NetworkStream netStream, string FileName)
        {
            byte[] SendingBuffer = null;
            try
            {
                client = Listener.AcceptTcpClient();
                netStream = client.GetStream();

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
                    netStream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                    netStream.Flush();
                }
                Fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
        
    }
}
