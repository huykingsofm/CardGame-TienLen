using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Json;

namespace Server{
    abstract public class Thing : Object{
        public abstract string Name{get;}
        public virtual void WriteLine(Object str, params object[] obj){
            Console.WriteLine("From {0} : {1}".Format(this.Name, str), obj);
        }
        public virtual void WriteLine(Object str){
            Console.WriteLine("From {0} : {1}".Format(this.Name, str));
        }
    }
}