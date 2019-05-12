using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Server
{
    static class Utils
    {
        public static string HashSHA1(String raw)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] inputbyte = Encoding.ASCII.GetBytes(raw);
            byte[] hashed = sha1.ComputeHash(inputbyte);
            String hashedstring = Encoding.ASCII.GetString(hashed);
            return hashedstring;
        }

        public static bool IsValidUsername(string username)
        {
            if (username.Length < 6 || username.Length > 18)
                return false;
            if (Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$") == false)
                return false;
            return true;
        }
        public static bool IsValidPassword(string pass)
        {
            if (pass.Length < 6 || pass.Length > 18)
                return false;
            return true;
        }
        public static object Copy(Object obj){
            IFormatter formater = new BinaryFormatter();
            MemoryStream mem = new MemoryStream();
            formater.Serialize(mem, obj);
            Object o = formater.Deserialize(mem);
            return o;
        }

        public static string Format(this string str, params object[] argvs){
            return String.Format(str, argvs);
        }
        public static void Shuffle<T>(this IList<T> list) {
            Random rng = new Random(); 
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }
    }
}
