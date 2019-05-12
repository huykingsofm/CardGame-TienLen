using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            OutdoorSession outdoor = new OutdoorSession();
            outdoor.Start();
            bool stop = false;
            while(stop == false){
                string str = Console.ReadLine();
                switch(str){
                    case "Close":
                        outdoor.Destroy();
                        stop = true;
                        break;
                }    
            }
        }
    }
}
