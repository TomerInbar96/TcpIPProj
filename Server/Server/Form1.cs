using SimpleTCP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPIPDemo
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }

        SimpleTcpServer server;

        private void Server_Load(object sender, EventArgs e)
        {
            server = new SimpleTcpServer();
            server.Delimiter = 0x13;
            server.StringEncoder = Encoding.UTF8;
            server.DataReceived += Server_DataReceived;
        }

        private void Server_DataReceived(object sender, SimpleTCP.Message e)
        {
            txtStatus.Invoke((MethodInvoker)delegate () 
            {
                txtStatus.Text += e.MessageString;
                e.ReplyLine(string.Format("You said: {0}", e.MessageString));
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            txtStatus.Text += "Server starting...";
            System.Net.IPAddress ip = System.Net.IPAddress.Parse(txtHost.Text);
            server.Start(ip, Convert.ToInt32(txtPort.Text));
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            CommonLibrary.TcpClass.StopService(server);
            //if (server.IsStarted)
            //{
            //    server.Stop();
            //}
        }

        private void btnGetFile_Click(object sender, EventArgs e)
        {
            string Path = @"C:\Users\guy\Desktop\FileToGet\Hello.bat";
            CommonLibrary.TcpClass.ServerReceiveFile(Convert.ToInt32(txtPort.Text), Path);
        }

        private void btnFileReply_Click(object sender, EventArgs e)
        {
            string Path = @"C:\Users\guy\Desktop\FileToGet\Hello.bat";
            CommonLibrary.TcpClass.HandleServerRequest(Convert.ToInt32(txtPort.Text));
        }
    }
}
