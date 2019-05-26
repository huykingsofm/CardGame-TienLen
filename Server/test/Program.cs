using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static public readonly char SPLIT_MESSAGE_CHAR = '|';
        static void Main(string[] args)
        {
            int[] a = new int[]{1, 2, 3};
            int[] b = (int[])a.Clone();
            b[1] = 4;
            foreach(var x in b)
                Console.WriteLine(x);
        }
    }
}
