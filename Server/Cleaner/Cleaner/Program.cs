using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Json;

namespace Cleaner
{
    class Program
    {
        static string[] ip = new string[2];
        static int[] port = new int[2];
        static int time;

        static Program()
        {
            JsonValue jsv = null;
            using (var f = new JsonReader("cleaner.ini"))
                jsv = f.Read();
            ip[0] = jsv["ip1"];
            ip[1] = jsv["ip2"];
            port[0] = jsv["port1"];
            port[1] = jsv["port2"];
            time = jsv["time"];
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Running...");
            while (true)
            {
                Thread.Sleep(time);
                for (int i = 0; i < 2; i++)
                {
                    if (CheckServerExist(ip[i], port[i]) == true)
                        continue;
                    Console.WriteLine("Cleaning {0}:{1}", ip[i], port[i]);
                    string server = ToServer(ip[i], port[i]);
                    string otherserver = ToServer(ip[1 - i], port[1 - i]);
                    GameCollection.__default__.Change(server, otherserver);
                    WorkingCollection.__default__.Change(server, "none");
                }
            }
        }

        static bool CheckServerExist(string ip, int port)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(ip, port);
                    tcpClient.Close();
                    return true;
                }
                catch (Exception)
                {
                    //Console.WriteLine("Port closed");
                    return false;
                }
            }
        }

        static string ToServer(string ip, int port)
        {
            return String.Format("{0}:{1}", ip, port);
        }
    }
}
