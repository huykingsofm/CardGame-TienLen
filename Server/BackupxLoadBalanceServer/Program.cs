using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Json;

namespace Server
{
    static class Program
    {
        public static readonly string socket;
        static Program(){
            using(var f = new JsonReader("server.ini")){
                JsonValue jsv = f.Read();
                string IP = jsv["IP"];
                int port = jsv["Port"];
                Program.socket = "{0}:{1}".Format(IP, port);
            }
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Write by Le Ngoc Huy");
            OutdoorSession outdoor = OutdoorSession.Create();
            Gate gate = Gate.Create(outdoor);
            outdoor.Start();
            gate.Start();
            bool stop = false;
            
            while(stop == false){
                string str = Console.ReadLine();
                switch(str){
                    case "Close":
                        gate.Stop();
                        outdoor.Destroy();
                        stop = true;
                        break;
                }    
            }
        }
    }
}
