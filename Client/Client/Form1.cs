using ClientLibrary;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ClientTcp myClient;
        SimpleTcpClient client; 

        private void Form1_Load(object sender, EventArgs e)
        {
            client = new SimpleTcpClient();
            client.StringEncoder = Encoding.UTF8;
            client.DataReceived += Client_DataReceived;
            this.myClient = new ClientTcp();
        }

        private void Client_DataReceived(object sender, SimpleTCP.Message e)
        {
            txtStatus.Invoke((MethodInvoker)delegate ()
            {
                txtStatus.Text += (e.MessageString + Environment.NewLine);
            });
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            client.WriteLineAndGetReply(txtMessage.Text, TimeSpan.FromSeconds(3));
        }

        private void SendFile_Click(object sender, EventArgs e)
        {
            string filePath = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
            this.myClient.ClientSendFile(filePath);
        }

        private void AskFile_Click(object sender, EventArgs e)
        {
            string filePath = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
            this.myClient.AskFileFromServer(filePath);
        }

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            string filePath = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
            CommonLibrary.TcpClass.deleteFile(filePath);
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnDisConnect.Enabled = true;
            btnConnect.Enabled = false;
            await this.myClient.Connect(txtHost.Text, Convert.ToInt32(txtPort.Text));
        }

        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            this.myClient.DisConnect();
            btnDisConnect.Enabled = false;
            btnConnect.Enabled = true;
        }
    }
}
