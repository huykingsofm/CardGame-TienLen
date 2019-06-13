using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace NavigatorServer
{
    class TcpClientModel 
    {
        const int MAX_BUFFER = 1024;
        TcpClient client;
        Stream stm;
        string IpOfServer;
        int port;
        bool IsDisconnect = false;

        public TcpClientModel(string ip, int port)
        {
            this.IpOfServer = ip;
            this.port = port;
            client = new TcpClient();
        }

        public virtual int Connect()
        {
            try
            {
                client.Connect(IPAddress.Parse(IpOfServer), port);
                stm = client.GetStream();
                return 1;
            }
            catch
            {
                throw new Exception();
            }
        }

        public int Send(string Msg)
        {
            try
            {
                if(Msg == null || Msg == "")
                    return -1;
                Byte[] ByteMsg = Encoding.ASCII.GetBytes(Msg);
                stm.Write(ByteMsg, 0, ByteMsg.Count());
                return 1;
            }
            catch
            {
                return -1;
            }
        }

        public string Receive()
        {
            try
            {
                Byte[] ByteMsg = new Byte[MAX_BUFFER];
                int len = stm.Read(ByteMsg, 0, MAX_BUFFER);
                if (len == 0)
                    return null;
                string Msg = Encoding.ASCII.GetString(ByteMsg, 0, len);
                if (Msg == "")
                    return null;
                return Msg;
            }
            catch
            {
                return null;
            }
        }
        public int DisconnectToServer()
        {
            if (IsDisconnect == false)
            {
                client.Close();
                IsDisconnect = true;
                return 1;
            }
            return -1;
        }
    }
}
