using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
   

    public class Class2
    {
        public int n1 {
            get;
            protected set;
        }
        public class Class1
        {
            internal protected int n;
            public Class1()
            {
                Class2 c2 = null;
                c2.n1 = 1;
            }
        } 
        Class1 c;
        public Class2()
        {
            c.n = 0;
        }
    }
}
