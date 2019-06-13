using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;

namespace NavigatorServer
{
    class Program
    {
        static void Main(string[] args)
        {
            JsonValue jsv = null;
            using (var f = new JsonReader("navigatorhost.ini"))
                jsv = f.Read();
            string ip = jsv["ip"];
            int port = jsv["port"];

            Navigator nav = new Navigator(ip, port);
            nav.StartServer();
            nav.SetMiddleConnection();
        }
        static Program()
        {
            JsonValue jsv = null;
            using (var f = new JsonReader("navigatorhost.ini"))
                jsv = f.Read();

        }
    }
}
