using CommonLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerLibrary
{
    public class ServerTcp
    {
        private NetworkStream _netstream;
        private TcpListener _listener;
        private CancellationTokenSource _token;

        private NetworkStream netStream
        {
            get
            {
                return this._netstream;
            }
            set
            {
                this._netstream = value;
            }
        }

        private TcpListener Listener
        {
            get
            {
                return this._listener;
            }
            set
            {
                this._listener = value;
            }
        }

        private CancellationTokenSource cToken
        {
            get
            {
                return this._token;
            }
            set
            {
                this._token = value;
            }
        }

        public ServerTcp(int Port)
        {
            Listener = new TcpListener(IPAddress.Any, (int)Port);
        }

        public ServerTcp()
        {
        }

        public void stopService()
        {
            if (Listener != null)
            {
                cToken.Cancel();
                Listener.Stop();
            }
        }

        public async Task StartServiceAsync(int Port)
        {
            this.Listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                Listener.Start();
                cToken = new CancellationTokenSource();
                cToken.Token.Register(() => Listener.Stop());
                await HandleServerRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task HandleServerRequest()
        {
            while (!cToken.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await Task.Run(() => Listener.AcceptTcpClientAsync(), cToken.Token);
                    await Task.Run(() => ProcessClientRequest(client), cToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static async Task ProcessClientRequest(object argument)
        {
            TcpClient client = (TcpClient)argument;
            ServerClientMessage recivedMessage;
            try
            {

                NetworkStream netstream = client.GetStream();

                recivedMessage = await CommonLibrary.TcpClass.GetMessageData(netstream);

                switch (recivedMessage.MyMessageType)
                {
                    case MessageType.AskForFile:
                        {
                            SendFileBack(netstream, recivedMessage);
                            break;
                        }
                    case MessageType.DownloadAndExe:
                        {
                            DownloadAndExeFile(recivedMessage, @"C:\Users\guy\Desktop\FileToGet\Hello.bat");
                            break;
                        }
                    case MessageType.DownloadAndExeRes:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SendFileBack(NetworkStream netStream, ServerClientMessage myMessage)
        {
            byte[] SendingBuffer = null;
            try
            {
                byte[] dataSend;
                List<byte> byteList = new List<byte>();

                string FileName = @"C:\Users\guy\Desktop\FileToGet\Hello.bat";

                // If I want to get the file path from my client.
                //string FileName = Encoding.ASCII.GetString(myMessage.MyData);

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

                myReply.MyData = byteList.ToArray();
                dataSend = myReply.serialize();
                netStream.Write(dataSend, 0, dataSend.Length);
                netStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
    }
}
