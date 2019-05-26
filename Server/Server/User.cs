using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class UserPermission : Permission{
        static public readonly long EDIT_SELF = Permission.PERMISSION_1;
        static public readonly long READ_SELF = Permission.PERMISSION_2;
        static public readonly long EDIT_OTHER = Permission.PERMISSION_3;
        static public readonly long READ_OTHER = Permission.PERMISSION_4;
        static public readonly long EDIT_SELF_PASS = Permission.PERMISSION_5;
        static public readonly long EDIT_OTHER_PASS = Permission.PERMISSION_6;
        static public readonly long READ_SELF_PASS = Permission.PERMISSION_7;
        static public readonly long READ_OTHER_PASS = Permission.PERMISSION_8;
        static public readonly long PLAY = Permission.PERMISSION_9;
        
        static public readonly long BASIC = READ_SELF | READ_OTHER;
        static public readonly long AUTHENTICATED = EDIT_SELF | READ_SELF | READ_OTHER | PLAY; 
        static public readonly long ADMINISTRATOR = READ_SELF | READ_OTHER_PASS | READ_OTHER;

        public UserPermission(Permission permission) : base(permission){
        }
        public UserPermission() : base(){
        }
    }
    public class User{
        //### ATTRIBUTES ###
        static public User __administrator__;
        static private List<User> LoggedIn;
        private UserPermission permission;

        // __lock__ sử dụng cho việc tạo ra tính độc nhất khi ghi, lấy dữ liệu
        public _lock_ __lock__ = null; 
        public String username
        {
            get;
            protected set;
        }
        public int money
        {
            get;
            protected set;
        }//public only in this file
      
        //- - - - - - - - -CONSTRUCTORS - - - - - -- - - - 
        static User(){ 
            // Thiết lập giá trị cho user administrator
            User.__administrator__ = new User();
            User.__administrator__.permission.SetNew(UserPermission.ADMINISTRATOR);
            User.__administrator__.username = "administrator";
            User.LoggedIn = new List<User>();
        }
        public User(){ 
            // Tạo User ảo và không cấp quyền
            this.username = null;
            this.money = 0;
            this.permission = new UserPermission(); // Không có quyền gì
        }
        public User(string username, int money) : this(){ 
            // Tạo user với thông tin ảo và không cấp quyền
            this.username = username;
            this.money = money;
        }
        public User(string username) : this(){
            // Tạo user với thông tin thật và không cấp quyền
            User tmp = UserCollection.__default__.GetInfo(__administrator__, username);
            this.CopyFrom(tmp);
        }
        public User(string username, string pass) : this(username) {
            // Tạo user với thông tin thực và xác thực, nếu xác thực thành công thì cấp quyền cho user, 
            // .. nếu không, không cấp quyền gì 
            lock (User.LoggedIn)
            {   
                if (username == "administrator")
                    throw new Exception("Administrator can not log in as normal user"); 
                
                bool bSuccess = UserCollection.__default__.Authenticate(__administrator__, username, pass);
                if (bSuccess == false)
                    throw new Exception("Incorrect username or password");
                
                // Nếu user đã đăng nhập rồi --> exception
                if (User.InLoggedIn(this.username))
                    throw new Exception("User has already logged in another place");

                if (bSuccess){
                    this.permission.SetNew(UserPermission.AUTHENTICATED);
                    User.LoggedIn.Add(this);
                }
            }
        }
        
        
        //- - - - - - - - - METHODS - - - - - - - - - -
        public void CopyFrom(User user){
            if (this == __administrator__)
                throw new Exception("User administrator cannot be coppied");

            this.username = user.username;
            this.money = user.money;
            this.__lock__ = user.__lock__;
            this.permission.SetNew(UserPermission.BASIC);
        }

        public User Clone(){
            if (this == __administrator__)
                throw new Exception("User administrator cannot be cloned");

            User tmp = new User();
            
            tmp.username = this.username;
            tmp.money = this.money;
            tmp.__lock__ = this.__lock__;
            tmp.permission.SetNew(UserPermission.BASIC);
            
            return tmp;
        }
        public void GetInfo(){
            // Lấy thông tin mới từ database, không thay đổi quyền của user
            User tmp = UserCollection.__default__.GetInfo(this, this.username);
            this.money = tmp.money;
        }
        public User GetInfo(string username){
            return UserCollection.__default__.GetInfo(this, username);
        }
        public void Authenticate(string pass){
            // Cấp thêm quyền cho user nếu xác thực danh tính
            lock ( User.LoggedIn ) {
                //if (this == __administrator__ || this.username == "administrator")
                //    throw new Exception("Administrator can not log in as normal user");

                bool bSuccess = UserCollection.__default__.Authenticate(__administrator__, 
                                                                        this.username, pass);
                if (bSuccess == false)
                    throw new Exception("Incorrect username or password");

                // Nếu user đã đăng nhập --> exception
                if (User.InLoggedIn(this.username))
                    throw new Exception("User has already logged in another place");
                
                this.permission.SetNew(UserPermission.AUTHENTICATED);
                User.LoggedIn.Add(this);
            }
        }
        public void Authenticate(string username, string pass) {
            this.username = username;
            this.Authenticate(pass);
        }
        public int ChangeMoney(int additionMoney){
            return UserCollection.__default__.ChangeMoney(this, this.username, additionMoney);
        }
        public void ChangePass(string oldPass, string newPass){

            // Bước 1 : Xác thực lại danh tính, cấp cho người dùng quyền sửa đổi password(tạm thời)
            if (UserCollection.__default__.Authenticate(__administrator__, this.username, oldPass))
                this.permission.Add(UserPermission.EDIT_SELF_PASS);

            // Bước 2 : Thay đổi mật khẩu
            UserCollection.__default__.ChangePass(this, this.username, newPass);

            // Bước 3 : Loại bỏ quyền được sửa đổi mật khẩu
            this.permission.Remove(UserPermission.EDIT_SELF_PASS);
        }
        public bool HavePermission(long permission){
            return this.permission.HavePermission(permission);
        }
        public void Destroy(){ // Hủy User và các quyền của nó
            lock(User.LoggedIn){
                this.username = null;
                this.money = 0;
                this.__lock__ = null;
                
                if (User.LoggedIn.Contains(this) == true){
                    User.LoggedIn.Remove(this);
                }

                this.permission.Remove(Permission.ALL_PERMISSION);
            }
        }
        static public bool InLoggedIn(string username){
            foreach (var user in User.LoggedIn){
                if (username == user.username)
                    return true;
            }
            return false;
        }
    }
}
