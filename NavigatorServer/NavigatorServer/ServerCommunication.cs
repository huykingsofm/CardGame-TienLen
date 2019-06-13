using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;
using System.Net.Sockets;

namespace NavigatorServer
{
    class ServerCommunication: TcpClientModel
    {
        static int[] NumOfSocket = new int[2];
        static string[] IP = new string[2];
        static int[] Port = new int[2];
        int ID;
        public static int time;
        public string ip;
        public int port;

        static ServerCommunication()
        {
            JsonValue jsv = null;
            using (var f = new JsonReader("navigator.ini"))
                jsv = f.Read();
            IP[0] = jsv["ip1"];
            IP[1] = jsv["ip2"];
            Port[0] = jsv["port1"];
            Port[1] = jsv["port2"];
            time = jsv["time"];
        }
        private ServerCommunication(string ip, int port): base (ip, port)
        {
            this.ip = ip;
            this.port = port;
            for (int i = 0; i < IP.Count(); i++)
                if (ip == IP[i] && port == Port[i])
                    ID = i;
        }

        public static ServerCommunication Navigate(string ip = null, int port = 0)
        {
            if (ip == null)
            {
                int min = ServerCommunication.NumOfSocket[0];
                int index = 0;
                for (int i = 0; i < IP.Count(); i++)
                    if (min > ServerCommunication.NumOfSocket[i])
                    {
                        index = i;
                        min = ServerCommunication.NumOfSocket[i];
                    }

                ServerCommunication sc = null;
                try
                {
                    if (CheckServer(IP[index], Port[index]) == false)
                        throw new Exception();
                    sc = new ServerCommunication(IP[index], Port[index]);
                    NumOfSocket[index]++;
                    sc.Connect();

                }
                catch
                {
                    sc = new ServerCommunication(IP[1 - index], Port[1 - index]);
                    NumOfSocket[1 - index]++;
                    sc.Connect();
                }

                return sc;
            }
            else
            {
                int index = 0;
                for (int i = 0; i < IP.Count(); i++)
                    if (ip == IP[i] && port == Port[i])
                    {
                        index = i;
                    }

                ServerCommunication sc = new ServerCommunication(IP[1 - index], Port[1 - index]);
                NumOfSocket[1 - index]++;
                sc.Connect();
                
                return sc;
            }
        }
        
        public void Disconnect()
        {
            try
            {
                if(base.DisconnectToServer() == 1)
                    NumOfSocket[ID]--;
            }
            catch
            {
                //Do nothing
            }
        }

        public static bool CheckServer(string ip, int port)
        {
            bool res = false;
            using (TcpClient tcp = new TcpClient())
            {
                IAsyncResult ar = tcp.BeginConnect(ip, port, null, null);
                System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
                try
                {
                    if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                    {
                        tcp.Close();
                        res = false;
                        throw new Exception();
                    }

                    tcp.EndConnect(ar);
                    res = true;
                }
                finally
                {
                    wh.Close();

                }
            }
            return res;
        }
    }
}
