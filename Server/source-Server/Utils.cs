using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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

        public static int IsValidUsername(string username)
        {
            int error = 0;
            if (username.Length < 6 || username.Length > 18)
                error &= 1;
            if (Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$") == false)
                error &= 2;
            return error;
        }
        public static int IsValidPassword(string pass)
        {
            int error = 0;
            if (pass.Length < 6 || pass.Length > 18)
                error &= 1;
            return error;
            User usr = null;
            usr.money = 3;
        }
    }
}
