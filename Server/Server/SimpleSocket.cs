using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server {
    public class SimpleSocket : Thing{
        public override string Name => "SimpleSocket";
        public const int MAX_ACCEPTED_SOCKET = 10;
        private static int count = 0;
        static int BUFFER_SIZE = 1024;
        private Socket socket = null;
        public SimpleSocket(Socket s){
            if (SimpleSocket.count >= SimpleSocket.MAX_ACCEPTED_SOCKET){
                s.Close();
                throw new Exception("Server can not accept any more client");
            }
            this.socket = s;
            this.socket.ReceiveBufferSize = BUFFER_SIZE;
            this.socket.SendBufferSize = BUFFER_SIZE;
            SimpleSocket.count++;
            this.WriteLine("The remain slot in server : {0}", SimpleSocket.MAX_ACCEPTED_SOCKET - SimpleSocket.count);   
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
            
            Console.WriteLine("From {0} : {1}".Format(this, str));
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
            Console.WriteLine("To {0} : {1}".Format(this, str));
            return true;
        }
        public void Close() {
            try{
                this.socket.Close();
            }
            catch(Exception e){
                this.WriteLine(e.Message);
            }
            finally{
                SimpleSocket.count--;
            }
            this.WriteLine("The remain slot in server : {0}", SimpleSocket.MAX_ACCEPTED_SOCKET - SimpleSocket.count);   
        }
        public override string ToString(){
            return this.socket.RemoteEndPoint.ToString();
        }
        public bool IsConnected(){
            try{
                bool part1 = this.socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (this.socket.Available == 0);

                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch{
                return false;
            }
        }
    }
}
