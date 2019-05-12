// Mục đích : Tạo ra một lớp duy nhất(UserDatabase) có thể cấp quyền sử dụng tài nguyên(database, ...) 
//..cho class User
//
//
//

class UserDatabase : Database {
    public class User{
        // Sử dụng : UserDatabase.User = new ....
        internal public bool authenticated; //public only in this file
        internal public string username; //public only in this file
        internal public float money; //public only in this file
        
        static List<User> InActive;
        static User Administator;
        //### CONSTRUCTOR
        internal User(){
            this.username = null;
            this.money = -1;
            this.authenticated = false;
        }

        public bool ChangeMoney(float additionMoney){
            if (this.authenticated)
                return UserDatabase.ChangeMoney(this, additionMoney);
        }

        public bool ChangePass(string oldPass, string newPass){
            if (this.authenticated)
                return UserDatabase.ChangePass(this, oldPass, newPass);
        }

        public User GetInfoAnotherUser(string username){
            if (this.authenticated)
                return UserDatabase.GetInfo(username);
        }
    }

    // ### ATTRIBUTE - không
    
    // ### CONSTRUCTOR 
    public UserDatabase(string serverpath, string dbName):base(serverpath, dbName){
        //None
    }

    // ### METHOD
    public bool IsExist(User user, string username){
        // Kiểm tra username có tồn tại hay không?
    }

    public bool Authorize(string username, string pass){
        // Xác nhận người dùng, trả về xác nhận được hay ko?
        return true/false;
    }

    public bool Authorize(User user, string pass){
        return this.Authorize(user.username, pass);
    }

    public User GetInfo(string username){
        // Lấy thông tin *username + money* của một user, nhưng ko cấp quyền cho người đó
        // Trả về user và thông tin nếu có tồn tại, nếu không trả về null hoặc thông báo lỗi
        if (this.IsExist(username) == true){
            User user = new User();
            user.username = username;
            user.money = this.Find<tiền>(username); // hàm của Database/MongoDatabase
        }
        return null;
    }

    public User Authenticate(string username, string pass){
        // Cấp quyền cho người dùng
        // Trả về User đã được cấp quyền
        User user = this.GetInfo(username);
        if (this.Authorize(username, pass) == true){
            user.authenticate = True; 
        }
        return user;
    }

    public void Authenticate(ref User user, string pass){
        user.authenticated = this.Authorize(user, pass);
    }

    public bool AddToDatabase(string username, string rawPasswd, float money){
        // Thêm người dùng với các thông tin, trả về có thêm được hay không, hoặc throw lỗi 
        // Cần kiểm tra sự tồn tại username, độ khả dụng của mật khẩu và tiền trước khi thêm
        this.InsertOne("...");
    }

    

    public bool ChangeMoney(ref User user, string additionMoney){
        // Thay đổi tiền của người dùng, người dùng đã được cấp quyền mới có thể thực hiện thay đổi 
        // Chỉ có thể thay đổi bằng cách tăng (additionMoney > 0) hoặc giảm (additionMoney < 0) số tiền hiện tại
        // Sau khi thay đổi, user sẽ nhận số tiền mới
        // Trả về True/False tương ứng có thành công hay không
        if (user.IsAuthenticated() == true){
            // Cần check additionMoney và currentMoney
            this.Update(username, newMoney);
            user.money = newMoney;
            return true;
        }
        return false;
    }

    public bool ChangePass(User user, string oldPass, string newPass){
        // Thay đổi mật khẩu người dùng, cần phải ` xác thực lại ` danh tính trước khi thay đổi
        // Không thay đổi quyền user trước và sau khi cập nhật pass
        // Trả về true/false tương ứng
        if (this.Authorize(user.username, oldPass) == True){
            this.Update(username, newPass);
            return True;
        }
        return False;
    }
}