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
        public static void Shuffle<T>(this IList<T> list, bool multishuffle = true, int seed = 30) {
            Random rng = new Random();

            int random = multishuffle ? rng.Next(1, rng.Next(1, seed)) : 1;
            
            for (int i = 0; i < random; i++){ 
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

        public static int CountRealInstance(this object[] array){
            int count = 0;
            foreach(var obj in array)
                if (obj != null)
                    count++;
            return count;
        }

        public static Client[] GetClients(this ClientSession[] clients){
            Client[] users = new Client[clients.Count()];
            
            for (int i = 0; i < clients.Count(); i++)
                users[i] = clients[i].client;
            
            return users;
        }

        public static int Where(this object[] arr, object element){
            // Return first element index in arr
            for(int i = 0; i < arr.Count(); i++)
                if (arr[i] == element)
                    return i;

            return -1;
        }

        public static int FindById(this Session[] arr, int id){
            for (int i = 0; i < arr.Count(); i++)
                if (arr[i] != null){
                    if (arr[i].id == id)
                        return i;
                }
            return -1;
        }

        public static Session GetById(this Session[] arr, int id){
            int index = arr.FindById(id);
            if (index == -1)
                throw new Exception("Session id is not in array");

            return arr[index];
        }
    }
}
