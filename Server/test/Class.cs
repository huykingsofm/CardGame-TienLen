using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    public class Class2
    {
        public int n;
        
        public Class2()
        {
        }

        public virtual void x(){
            Console.WriteLine("x2");
        }
        public virtual void In(){
            Console.WriteLine("Class2");
            this.x();
        }
    }

    public class Class1 : Class2{

        public override void x(){
            Console.WriteLine("x1");
        }
        public override void In(){
            Console.WriteLine("Class1");
            base.In();
        }
    }
}
