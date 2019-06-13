using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace NavigatorServer
{
    class SocketModel
    {
        Socket socket;
        const int MAX_BUFFER_SIZE = 1024;
        
        public SocketModel(Socket socket)
        {
            this.socket = socket;
        }

        public int Send(string Msg)
        {
            try
            {
                if (Msg == null || Msg == "")
                    return -1;
                Byte[] ByteMsg = Encoding.ASCII.GetBytes(Msg);
                socket.Send(ByteMsg);
                return 1;
            }
            catch
            {
                return -1;
            }
        }

        public string Receive()
        {
            try {
                byte[] ByteMsg = new byte[MAX_BUFFER_SIZE];
                int len = socket.Receive(ByteMsg);
                if (len == 0)
                    return null;
                string Msg = Encoding.ASCII.GetString(ByteMsg, 0, len);
                return Msg;
            }
            catch
            {
                return null;
            }
        }

        private void Close()
        {
            socket.Close();
        }
    }
}
