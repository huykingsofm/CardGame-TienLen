using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server {
    class SocketModel {
        static int BUFFER_SIZE = 100;
        private Socket socket;
      

        public SocketModel(Socket s) {
            socket = s;
        }
       

        public String GetRemoteEndpoint() {
            string str = null;
            string remoteEndPoint = null;
            try {
                str = Convert.ToString(socket.RemoteEndPoint);
                remoteEndPoint = str;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
            return str;
        }

        public String ReceiveData() {

            String str = null;
            byte[] byteReceive = new byte[BUFFER_SIZE]; 
            try {
                int len = socket.Receive(byteReceive);
                str = System.Text.Encoding.ASCII.GetString(byteReceive, 0, len);
                Console.WriteLine("From " + this.GetRemoteEndpoint()
                    + " :" + str);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
            return str;
        }

        public int SendData(string str) {
            try {
                ASCIIEncoding encoding = new ASCIIEncoding();
                socket.Send(encoding.GetBytes(str));

                Console.WriteLine("To " + this.GetRemoteEndpoint()
                    + " :" + str);
                return 1;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        public void CloseSocket() {
            socket.Close();
        }
    }
}
