using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server {
    public class Permission {
        static public readonly long PERMISSION_1 = 1;
        static public readonly long PERMISSION_2 = 2;
        static public readonly long PERMISSION_3 = 4;
        static public readonly long PERMISSION_4 = 8;
        static public readonly long PERMISSION_5 = 16;
        static public readonly long PERMISSION_6 = 32;
        static public readonly long PERMISSION_7 = 64;
        static public readonly long PERMISSION_8 = 128;
        static public readonly long PERMISSION_9 = 256;
        static public readonly long PERMISSION_10 = 512;
        static public readonly long PERMISSION_11 = 1024;
        static public readonly long ALL_PERMISSION = Int32.MinValue; // All "1"
        public long permission {private set; get;}
        public Permission(){
            // nothing
        }
        public Permission(Permission a){
            this.permission = a.permission;
        }

        public Permission Clone(){
            return new Permission(this);
        }

        public void Add(long permission){
            this.permission |= permission;
        }
        public void Add(Permission permission){
            this.Add(permission.permission);
        }
        public void SetNew(long permission){
            this.permission = permission;
        }

        public void SetNew(Permission permission){
            this.SetNew(permission.permission);
        }
        public void Remove(long permission){
            if (this.HavePermission(permission) == false)
                return;
            
            this.permission = this.permission & (~permission);
        }

        public void Remove(Permission permission){
            this.Remove(permission.permission);
        }
        public bool HavePermission(long permission){
            return (this.permission & permission) != 0;
        }
        public bool HavePermission(Permission permission){
            return this.HavePermission(permission.permission);
        }
    }
}