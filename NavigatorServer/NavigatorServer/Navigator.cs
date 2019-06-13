using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Json;

namespace NavigatorServer
{
    class Navigator
    {
        const int MAX_SOCKET = 100;
        TcpServer NaviServer;
        TcpClientModel[] NaviClient;
        SocketModel[] sockets;


        public Navigator(string ip, int port)
        {
            NaviServer = new TcpServer(ip, port);
            sockets = new SocketModel[MAX_SOCKET];
            NaviClient = new TcpClientModel[MAX_SOCKET];
            
            for (int i = 0; i < MAX_SOCKET; i++)
            {
                sockets[i] = null;
            }
        }

        public void StartServer()
        {
            NaviServer.Start();
            Console.WriteLine("Start Server");
        }

        private int GetFreeSocket()
        {
            for (int i = 0; i < MAX_SOCKET; i++)
                if (sockets[i] == null)
                    return i;
            return -1;
        }

        public void SetMiddleConnection()
        {
            while (true)
            {
                SimpleSocket s = NaviServer.AcceptSimpleSocket();
                if (s == null)
                    continue;
                Communication communication = new Communication(s);
                communication.Serve();
            }
        }

    }
}
