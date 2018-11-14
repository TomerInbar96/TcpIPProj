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

        SimpleTcpClient client; 

        private void btnConnect_Click(object sender, EventArgs e)
        {
            client.Connect(txtHost.Text, Convert.ToInt32(txtPort.Text));
            btnConnect.Enabled = false; 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            client = new SimpleTcpClient();
            client.StringEncoder = Encoding.UTF8;
            client.DataReceived += Client_DataReceived;
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
            CommonLibrary.TcpClass.ClientSendFile(filePath, txtHost.Text, Convert.ToInt32(txtPort.Text));
        }

        private void AskFile_Click(object sender, EventArgs e)
        {
            string filePath = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
            CommonLibrary.TcpClass.AskFileFromServer(filePath, txtHost.Text, Convert.ToInt32(txtPort.Text));
        }

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            string filePath = @"C:\Users\guy\Desktop\FileToSendFromServer\OpenNotepad++.bat";
            CommonLibrary.TcpClass.deleteFile(filePath);
        }
    }
}
