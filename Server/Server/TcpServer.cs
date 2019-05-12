using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace Server
{
    class TcpServer : TcpListener
    {
        int timeout;
        public TcpServer(String ip, int p, int timeout = 1000) : base(IPAddress.Parse(ip), p){
            // nothing
            this.timeout = timeout;
        }
        public int Listen()
        {
            try
            {
                this.Start();
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }

        }
        public SimpleSocket AcceptSimpleSocket()
        {
            SimpleSocket socket = null;
            if (this.Pending())
                socket = new SimpleSocket(this.AcceptSocket());
            else
                Thread.Sleep(this.timeout);

            if (socket != null)
                Console.WriteLine(socket);
            return socket;
        }
    }
}
