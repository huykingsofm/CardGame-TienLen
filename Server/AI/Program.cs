using System;
using System.IO;
using System.Linq;

namespace AI
{
    class Program
    {
        static void Main(string[] args)
        {
            string content;
            using(var f = new StreamReader(args[0]))
                content = f.ReadLine();
            
            string[] cards = content.Split(' ');
            string[] newcards = new string[cards.Count()];
            for (int i = 0; i < newcards.Count(); i++)
                newcards[i] = "0";

            for (int i = 0; i < cards.Count(); i++){
                if (cards[i] == "1"){
                    newcards[i] = "1";
                    break;
                }
            }
                
            
            using(var f = new StreamWriter(args[1]))
                f.WriteLine(String.Join(' ', newcards));

            return;
        }
    }
}
