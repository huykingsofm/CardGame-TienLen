using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NavigatorServer
{
    class Communication
    {
        private SimpleSocket clientCommunication;
        private ServerCommunication serverCommunication;
        private bool IsConnectionStop;
        Queue<string> MsgQueue = new Queue<string>();
        string token;

        public Communication(SimpleSocket s)
        {
            clientCommunication = s;
        }

        public void Connect()
        {
            serverCommunication = ServerCommunication.Navigate();
        }

        public void ClientToServer()
        {
            while (IsConnectionStop == false)
            {
                string Msg = clientCommunication.Receive();
                if (Msg == null)
                {
                    IsConnectionStop = true;
                    serverCommunication.Disconnect();
                    clientCommunication.Close();
                    break;
                }
                while (true)
                {
                    if (serverCommunication.Send(Msg) == 1)
                        break;
                    Thread.Sleep(300);
                }
            }
        }

        private void ServerToClient()
        {
            string last_message = null;

            while (IsConnectionStop == false)
            {
                string all_message = this.serverCommunication.Receive();

                

                if (all_message == null)
                {
                    serverCommunication.Disconnect();
                    if (IsConnectionStop == false)
                    {
                        Thread.Sleep(ServerCommunication.time);
                        serverCommunication = ServerCommunication.Navigate(
                            serverCommunication.ip, serverCommunication.port
                            );
                        if (token != null)
                            serverCommunication.Send("Restore:" + token + "|");
                    }
                    continue;
                }
                Thread.Sleep(50);

                if (last_message != null)
                    all_message = last_message + all_message;

                // Nếu message cuối cùng vẫn chưa hoàn tất, đặt flag = 1
                int flag = all_message.Last() != '|' ? 1 : 0;

                string[] message = all_message.Split('|');

                if (flag == 1)
                    // Lưu giữ phần hiện tại của message cuối cùng
                    last_message = message.Last();
                else
                    last_message = null;

                // Gửi các thông điệp đã hoàn thiện đến bản thân
                for (int i = 0; i < message.Count() - 1; i++)
                {
                    string[] temp = message[i].Split(':');
                    if (temp[0] == "Token")
                    {
                        token = temp[1];
                        continue;
                    }
                    clientCommunication.Send(message[i] + "|");
                }

            }
        }

        public void Serve()
        {
            this.Connect();
            Thread t1 = new Thread(ClientToServer);
            t1.Start();
            Thread t2 = new Thread(ServerToClient);
            t2.Start();
        }

        
       
    }
}