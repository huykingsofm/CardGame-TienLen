using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    class TcpServer
    {
        private IPAddress IP;
        private int Port;
        private TcpListener tcpServer;

        public TcpServer(String ip, int p)
        {
            this.IP = IPAddress.Parse(ip);
            this.Port = p;
            tcpServer = new TcpListener(this.IP, this.Port);
        }

        public int Listen()
        {
            try
            {
                tcpServer.Start();
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }

        }

        public SocketModel SetUpANewConnection()
        {
            SocketModel socket = new SocketModel(tcpServer.AcceptSocket());
            return socket;
        }

        public void Shutdown()
        {
            this.tcpServer.Stop();
        }
    }
}
