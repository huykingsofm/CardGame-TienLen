using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server {
    public class SimpleSocket : Object{
        static int BUFFER_SIZE = 1024;
        private Socket socket = null;
        public SimpleSocket(Socket s){
            this.socket = s;
            this.socket.ReceiveBufferSize = BUFFER_SIZE;
            this.socket.SendBufferSize = BUFFER_SIZE;
        }
        
        public String Receive() {
            String str = null;
            byte[] byteReceive = new byte[BUFFER_SIZE]; 
            try {
                int len = this.socket.Receive(byteReceive);
                str = Encoding.ASCII.GetString(byteReceive, 0, len);
            } catch (Exception e) {
                Console.WriteLine("{0} : {1}".Format(e.Message, e.Source));
                return null;
            }

            if (str == "")
                return null;
            
            Console.WriteLine("From {0} : {1}".Format(this.socket.RemoteEndPoint, str));
            return str;
        }

        public bool Send(string str) {
            try {
                ASCIIEncoding encoding = new ASCIIEncoding();
                this.socket.Send(encoding.GetBytes(str + "|"));
            } 
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            Console.WriteLine("To " + this.socket.RemoteEndPoint.ToString()
                    + " :" + str);
            return true;
        }
        public void Close() {
            this.socket.Close();
        }
        public override string ToString(){
            return this.socket.RemoteEndPoint.ToString();
        }
    }
}
