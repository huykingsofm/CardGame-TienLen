using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace NavigatorServer
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
                Console.WriteLine("From TcpServer : ", e);
                return -1;
            }

        }
        public SimpleSocket AcceptSimpleSocket()
        {
            Thread.Sleep(this.timeout);

            SimpleSocket socket = null;
            if (this.Pending())
                try {
                    socket = new SimpleSocket(this.AcceptSocket());
                }
                catch (Exception e) {
                    Console.WriteLine("From Server : ", e.Message);
                    return null;
                }

            if (socket != null)
                Console.WriteLine(socket);
                
            return socket;
        }
    }
}
