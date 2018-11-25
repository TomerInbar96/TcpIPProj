using CommonLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class ClientTcp
    {
        private NetworkStream _netstream;
        private TcpClient _client;
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

        private TcpClient myClient
        {
            get
            {
                return this._client;
            }
            set
            {
                this._client = value;
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


        /// <summary>
        /// Ctor
        /// </summary>
        public ClientTcp()
        {
            this.myClient = null;
            this.netStream = null;
            this.cToken = null;
        }

        /// <summary>
        /// Connect to the server stream
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        /// <returns></returns>
        public async Task Connect(string Host, int Port)
        {
            try
            {
                this.myClient = new TcpClient(Host, Port);
                this.netStream = this.myClient.GetStream();
                this.cToken = new CancellationTokenSource();
                cToken.Token.Register(() => this.myClient.Close());

                await HandleClientRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Close the client stream
        /// </summary>
        public void DisConnect()
        {
            if (this.myClient != null && this.myClient.Connected)
            {
                this.cToken.Cancel();
                this.netStream.Close();
                this.myClient.Close();
            }
        }

        /// <summary>
        /// Ask file from the server
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public async Task AskFileFromServer(string FileName)
        {
            try
            {
                if (this.myClient.Connected)
                {
                    byte[] data = Encoding.ASCII.GetBytes(FileName);
                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.AskForFile, data.Length, data);

                    data = myMessage.serialize();

                    await this.netStream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Send file to the server
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public async Task ClientSendFile(string FileName)
        {
            byte[] SendingBuffer = null;
            try
            {
                if (this.myClient.Connected)
                {
                    List<byte> byteList = new List<byte>();
                    byte[] dataSend;

                    FileStream Fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                    int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(1024)));
                    int TotalLength = (int)Fs.Length, CurrentPacketLength;

                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.DownloadAndExe, TotalLength);

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
                        await Fs.ReadAsync(SendingBuffer, 0, CurrentPacketLength);
                        byteList.AddRange(SendingBuffer);
                    }

                    Fs.Close();
                    myMessage.MyData = byteList.ToArray();

                    dataSend = myMessage.serialize();

                    await this.netStream.WriteAsync(dataSend, 0, dataSend.Length);
                    await this.netStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Handling requests from the clients
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Host"></param>
        /// <param name="Port"></param>
        private async Task HandleClientRequest()
        {
            try
            {
                ServerClientMessage recivedMessage;

                while (!this.cToken.Token.IsCancellationRequested)
                {
                    recivedMessage = await TcpClass.GetMessageData(this.netStream);

                    if (!this.cToken.Token.IsCancellationRequested)
                    {
                        switch (recivedMessage.MyMessageType)
                        {
                            case MessageType.DownloadAndExe:
                                {
                                    string FileName = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
                                    TcpClass.DownloadAndExeFile(recivedMessage, FileName);
                                    break;
                                }

                            default:
                                {
                                    Console.WriteLine("Invalid message type");
                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                this.netStream.Close();
                this.myClient.Close();
            }
        }
    }
}
