using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace client {
    public class TcpClientModel {
        static ASCIIEncoding encoding = new ASCIIEncoding();
        static int MAX_BUFFER_SIZE = 1024;
        static int SERVER_PORT = 2019;
        static String SERVER_IP = "172.30.76.118";

        private TcpClient _tcpClient;
        private Stream _stream;

        public TcpClientModel() {
            this._tcpClient = new TcpClient();
            this._stream = null;
        }

        public int ConnectToServer() {
            try {
                this._tcpClient.Connect(SERVER_IP, SERVER_PORT);
                this._stream = this._tcpClient.GetStream();

                return 1; //Success
            } catch(Exception ex) {
                Console.WriteLine("ConnectToServer Error: " + ex.StackTrace);
                return -1; //Failed
            }
        }

        public int SendRequest(String req) {
            try {
                byte[] bytesSend = encoding.GetBytes(req + "|");
                this._stream.Write(bytesSend, 0, bytesSend.Length);

                return 1; //Success
            } catch(Exception ex) {
                Console.WriteLine("SendRequest Error: " + ex.StackTrace);
                return -1; //Failed
            }
        }

        public String ReceiveResponse() {
            String res = "";
            try {
                byte[] bytesReceive = new byte[MAX_BUFFER_SIZE];
                int length = this._stream.Read(bytesReceive, 0, MAX_BUFFER_SIZE);

                
                for(int i = 0; i < length; i++) {
                    res += Convert.ToChar(bytesReceive[i]);
                }

            } catch(Exception ex) {
                Console.WriteLine("ReceiveResponse Error: " + ex.StackTrace);
                return null;
            }
            
            if (res == "") return null;
            return res;
        }

        public void DisConnectToServer() {
            this.SendRequest(RequestFormat.DISCONNECT());
        }
    }
}
