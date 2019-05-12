using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server {
    public class Permission {
        // Class dùng để thể hiện thực thể có quyền nào đó
        public String name{ get; private set;}
        public Permission(){
            this.name = null;
        }
        public Permission(String PermissionName){
            this.name = PermissionName;
        }
    }

    public class RelationshipPermission : List<Permission>{
        public Permission permission;
    
        public RelationshipPermission() : base(){
            this.permission = null;
        }

        public RelationshipPermission(Permission permission) : base(){
            this.permission = permission;
        }

        public RelationshipPermission(String PermissionName) : base(){
            this.permission = new Permission(PermissionName);
        }
        public String Name(){
            return this.permission.name;
        }
    }
    public class PermissionsSet { // : Kể thừa từ Graph (đồ thị)

        // Mục đích : Thể hiện một tập hợp các quyền của một thực thể
        // Các quyền được thể hiện thành một đồ thị, có quan hệ lẫn nhau
        // Một mối quan hệ được biểu diễn là P1 --> P2, nghĩa là để thực thể
        // .. có quyền P2 thì nó cần có quyền P1 trước
        // .. Điều này cũng có nghĩa là nếu có nhiều mối quan hệ dạng Pi --> P
        // .. thì để thực thể có quyền P, nó cần có tất cả các quyền P

        //##### SOME EXEPTIONS ####

        static public readonly Exception EXISTENCE_PERMISSION 
            = new Exception("Permission have already existed");
        static public readonly Exception INEXISTENCE_PERMISSION
            = new Exception("Permission don't exist");

        List<RelationshipPermission> RelationshipSet;

        public PermissionsSet(){
            this.RelationshipSet = new List<RelationshipPermission>();
        }

        public bool  IsExisted(String PermissionName){
            // Kiểm tra quyền tên `PermissionName` có tồn tại trong tập hợp không?
            foreach(RelationshipPermission permission in this.RelationshipSet){
                if (permission.Name() == PermissionName)
                    return true;
            }
            return false; 
        }
        public bool  IsExisted(Permission permission){
            // Kiểm tra quyền có tồn tại không
            return this. IsExisted(permission.name);
        }
        public bool  IsExisted(RelationshipPermission permission){
            // Tương tự  IsExisted(Permission)
            return  IsExisted(permission.Name());
        }

        private RelationshipPermission GetInstance(String PermissionName){
            foreach(RelationshipPermission permission in this.RelationshipSet){
                if (permission.Name() == PermissionName)
                    return permission;
            }
            return null;
        }

        public void AddPermission(String PermissionName){
            // Thêm một quyền vào tập hợp
            // Nếu quyền đã tồn tại, trả về một lỗi
            if (this. IsExisted(PermissionName) == true)
                throw EXISTENCE_PERMISSION;

            this.RelationshipSet.Add(new RelationshipPermission(PermissionName));
        }

        public void AddPermission(Permission permission){
            // Một Overload
            this.AddPermission(permission.name);
        }

        public void AddPermission(RelationshipPermission permission){
            // Một Overload
            this.AddPermission(permission.Name());
        }

        public void AddRelationship(String PermissionNameFrom, String PermissionNameTo){
            // Mục đích : Thêm mối quan hệ S1-->S2 vào tập hợp
            
            // Bước 1 : Kiểm tra sự tồn tại của các quyền
            if (this. IsExisted(PermissionNameFrom) == false){
                throw INEXISTENCE_PERMISSION;
            }

            // Bước 2 : Thêm mối quan hệ
            foreach(RelationshipPermission permission in this.RelationshipSet){
                if (permission.Name() == PermissionNameFrom)
                    permission.Add(new Permission(PermissionNameTo));
            }
        }

        public void AddRelationship(Permission PermissionFrom, Permission PermissionTo){
            // Một Overload
            this.AddRelationship(PermissionFrom.name, PermissionTo.name);
        }

        public void AddRelationship(RelationshipPermission PermissionFrom, 
                                    RelationshipPermission PermissionTo){
            // Một Overload
            this.AddRelationship(PermissionFrom.Name(), PermissionTo.Name());
        }

        
        public bool HavePermission(String PermissionName){
            // Kiểm tra thực thể có quyền tên là PermissionName hay không
            // Sử dụng thuật toán tìm kiếm BFS để kiểm tra trên các mối quan hệ
            List<Permission> Visited = new List<Permission>();
            Queue<RelationshipPermission> Q = new List<RelationshipPermission>();
            
            // Thêm tất cả các "directly" permission vào Queue và danh sách đã thăm 
            foreach(RelationshipPermission permission in this.RelationshipSet){
                Q.Enqueue(permission);
                Visited.Add(permission.permission);
            }

            while(Q.Count() > 0){
                RelationshipPermission currentPermission = Q.Dequeue();
                if (currentPermission.Name() == PermissionName)
                    return true;
            }

            return true;
        }
    }   
}