using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class TcpClass
    {
        public static void StopService(SimpleTcpServer server)
        {
            if (server.IsStarted)
            {
                server.Stop();
            }
        }
    }
}
