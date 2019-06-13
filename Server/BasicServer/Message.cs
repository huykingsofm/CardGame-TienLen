using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;

namespace Server{
    public class Message : Object{
        public string who{get; private set;}
        public string which{get; private set;}
        public int id{get; private set;}
        public string name{get; private set;}
        public string[] args{get; private set;}
        private Message(string message){
            string[] tmp = message.Split('=');
            string next = null;
            if (tmp.Count() == 2){
                this.who = tmp[0];
                next = tmp[1];
                tmp = this.who.Split('-');
                if (tmp.Count() == 2){
                    this.which = tmp[0];
                    this.id = Int32.Parse(tmp[1]);
                }
                else{
                    this.which = null;
                    this.id = Int32.Parse(tmp[0]);
                }
            }
            else{
                this.who = null;
                this.which = null;
                this.id = -1;
                next = tmp[0];
            }

            tmp = next.Split(':');
            this.name = tmp[0];
            
            if (tmp.Count() == 2){
                next = tmp[1];
            }
            else{
                next = null;
            }

            if (next == null)
                this.args = null;
            else
                this.args = next.Split(',');
        }
        public static Message Create(string message){
            string[] tmp = null;
            tmp = message.Split('=');
            if (tmp.Count() > 2)
                return null;
            
            tmp = message.Split(':');
            if (tmp.Count() > 2)
                return null;

            Message m = new Message(message);
            return m;
        }
        public override string ToString(){
            string str = "";
            if (this.who != null)
                str += this.who + "=";
            str += this.name;
            if (this.args != null){
                string t = String.Join(',', this.args);
                str += ":" + t;
            }
            return str;
        }
        public string MessageOnly(){
            string str = "" + this.name;
            if (this.args != null){
                string t = String.Join(',', this.args);
                str += ":" + t;
            }
            return str;
        }
    }
}