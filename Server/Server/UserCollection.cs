using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server
{
    public class UserCollection
    {
        static public readonly Exception E_USER_NOT_EXIST = new Exception("User is not exist");
        static public readonly Exception E_USER_EXISTED = new Exception("User is existed");
        static public readonly Exception E_INVALID_USERNAME = new Exception("Invalid username");
        static public readonly Exception E_INVALID_PASSWORD = new Exception("Invalid password");
        static public readonly Exception E_INVALID_MONEY = new Exception("Money must be positive");
        static public readonly Exception E_TOO_BIG_MONEY = new Exception("Money is too big");
        static public readonly Exception E_DONT_HAVE_PERMISSION = new Exception("Dont have permission");
        static public readonly Exception E_ERROR_IN_DATABASE = new Exception("Something is wrong in database");
    
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static UserCollection __default__ = new UserCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public UserCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("users");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname){
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
        }
        public bool IsExist(User representation, string username){
            /* 
            # Mục đích : Kiểm tra username có tồn tại hay không?
            # Trả về true nếu user có tồn tại, ngược lại trả về false
            */
                
            // Nếu user có quyền và tự kiểm tra chính mình, trả về true
            if (representation.username == username)
                if (representation.HavePermission(UserPermission.READ_SELF) == true)
                    return true;
                else 
                    throw E_DONT_HAVE_PERMISSION;

            // Nếu user kiểm tra người khác, kiểm tra quyền READ_OTHER
            if (representation.HavePermission(UserPermission.READ_OTHER) == false)
                throw E_DONT_HAVE_PERMISSION;

            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return true;

            else if (result.Count() == 0)
                return false;
            throw E_ERROR_IN_DATABASE;
        }
        public User GetInfo(User representation, string username){
            /* 
            # Mục đích : Lấy thông tin *username + money* của một user, cấp một số quyền cơ bản, 
            # .. ko câp quyền tự chỉnh sửa
            # Trả về user và thông tin nếu có tồn tại, nếu không thông báo lỗi
            */

            // Nếu user tự lấy thông tin của chính mình, kiểm tra quyền READ_SELF
            if (representation.username == username){
                if (representation.HavePermission(UserPermission.READ_SELF) == false)
                    throw E_DONT_HAVE_PERMISSION;
            }
            // Nếu kiểm tra thông tin người khác, kiểm tra quyền READ_OTHER
            else if (representation.HavePermission(UserPermission.READ_OTHER) == false)
                throw E_DONT_HAVE_PERMISSION;
                    
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                User user = new User(username, result[0]["money"].AsInt32);
                return user; 
            }
            throw E_USER_NOT_EXIST;
        }
        public string GetHashPassword(User representation, string username){
            /* 
            # Mục đích : Lấy thông tin *hashed passwd* của một user
            # Trả về pw đã được hash.
            */

            // Nếu user tự lấy thông tin của chính mình, kiểm tra quyền READ_SELF
            if (representation.username == username){
                if (representation.HavePermission(UserPermission.READ_SELF_PASS) == false)
                    throw E_DONT_HAVE_PERMISSION;
            }
            // Nếu kiểm tra thông tin người khác, kiểm tra quyền READ_OTHER
            else if (representation.HavePermission(UserPermission.READ_OTHER_PASS) == false)
                throw E_DONT_HAVE_PERMISSION;
                    
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return result[0]["password"].AsString;

            throw E_USER_NOT_EXIST;
        }
        public bool Authenticate(User representation, string username, string pass){
            /*  
            # Mục đích : Xác nhận người dùng
            # Trả về true nếu xác thực chính xác, ngược lại trả về false
            # Các quyền được cấp là : xem UserPermission.AUTHENTICATED
            # Thông báo nếu có lỗi xảy ra
            */

            // Nếu user tự cấp quyền cho chính mình, kiểm tra quyền READ_SELF_PASS
            if (representation.username == username){
                if (representation.HavePermission(UserPermission.READ_SELF_PASS) == false)
                    throw E_DONT_HAVE_PERMISSION;
            }
            // Nếu user cấp quyền cho người khác, kiểm tra quyền READ_OTHER_PASS
            else if (representation.HavePermission(UserPermission.READ_OTHER_PASS) == false)
                throw E_DONT_HAVE_PERMISSION;

            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                if (result[0]["password"] == Utils.HashSHA1(pass))
                    return true;

                return false; // Đúng tên nhưng Sai mật khẩu
            }

            if (result.Count() == 0)
                return false; // User không tồn tại

            throw E_ERROR_IN_DATABASE;
        }   
        public string AuthenticateByToken(string token, Client client){
            /*
             * Xác thực người dùng bằng cách sử dụng token
             */
            string username = null;
            try{
                username = TokenCollection.__default__.GetUsername(token);
            }
            catch{
                throw new Exception("Token dont match with any user");
            }
            
            string hasedpw = UserCollection.__default__.GetHashPassword(
                User.__administrator__,
                username
            );

            string newtoken = Utils.HashMd5(hasedpw + client.socket.GetIP());
            if (token != newtoken)
                throw new Exception("Your token is invalid");
            
            return username;
        }
        public void AddToDatabase(User representation, 
                                string username, string rawPasswd, int money)
        {
            /*  
            # Mục đích : Thêm người dùng với các thông tin
            # Thông báo lỗi nếu không thể thêm 
            # Cần kiểm tra sự tồn tại username, độ khả dụng của mật khẩu và tiền trước khi thêm
            # Chỉ có __administrator__ mới có thể thêm
            */
            lock(this){
                if (representation != User.__administrator__)
                    throw E_DONT_HAVE_PERMISSION;
                
                if (Utils.IsValidUsername(username) == false)
                    throw E_INVALID_USERNAME;
                if (Utils.IsValidPassword(rawPasswd) == false)
                    throw E_INVALID_PASSWORD;
                if (money < 0)
                    throw E_INVALID_MONEY;
                if (this.IsExist(representation, username))
                    throw E_USER_EXISTED;

                string hashedPassword = Utils.HashSHA1(rawPasswd);
                BsonDocument newUser = new BsonDocument {
                    { "username", username },
                    { "password", hashedPassword },
                    { "money", money }
                };

                this.collection.InsertOne(newUser);
            }
        }
        public int ChangeMoney(User representation,
                                string username, int additionMoney){
            /* 
            # Mục đích : Thay đổi tiền của người dùng 
            # Chỉ có thể thay đổi bằng cách tăng (additionMoney > 0) hoặc 
            # ..giảm (additionMoney < 0) số tiền hiện tại
            # Sau khi thay đổi, user sẽ nhận số tiền mới
            # Khi số tiền giảm đi lớn hơn số tiền hiện tại, tiền của user sẽ trừ về 0
            */

            // Nếu user tự chỉnh sửa cho chính mình, kiểm tra quyền EDIT_SELF
            if (representation.username == username){
                if (representation.HavePermission(UserPermission.EDIT_SELF) == false)
                    throw E_DONT_HAVE_PERMISSION;
            }
            // Nếu user chỉnh sửa cho người khác, kiểm tra quyền EDIT_OTHER
            else if (representation.HavePermission(UserPermission.EDIT_OTHER) == false)
                throw E_DONT_HAVE_PERMISSION;

            var query = Builders<BsonDocument>.Filter.Eq("username", username);

            // Lấy thông tin chính xác ở database hiện tại
            User tmp = this.GetInfo(representation, username);

            if (tmp.money + additionMoney < 0){ // Nếu số tiền trừ đi vượt quá số tiền hiện có
                additionMoney = -tmp.money;    
            }

            if (tmp.money + additionMoney < 0 && additionMoney > 0) // Xử lý tràn số
                throw E_TOO_BIG_MONEY;

            var update = Builders<BsonDocument>.Update
                .Set("money", tmp.money + additionMoney);
            this.collection.UpdateOne(query, update);
            return additionMoney;
        }
        public void ChangePass(User representation,
            string username, String newPass){
            /*
            # Mục đích : Thay đổi mật khẩu của người dùng, người dùng cần có quyền EDIT_SELF_PASS
            # Thông báo lỗi nếu không thể thay đổi
            */
                
            // Nếu user tự chỉnh sửa cho chính mình, kiểm tra quyền EDIT_SELF_PASS
            if (representation.username == username){
                if (representation.HavePermission(UserPermission.EDIT_SELF_PASS) == false)
                    throw E_DONT_HAVE_PERMISSION;
            }
            // Nếu user chỉnh sửa cho người khác, kiểm tra quyền EDIT_OTHER_PASS
            else if (representation.HavePermission(UserPermission.EDIT_OTHER_PASS) == false)
                throw E_DONT_HAVE_PERMISSION;

            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            var update = Builders<BsonDocument>.Update
                .Set("password", Utils.HashSHA1(newPass));

            this.collection.UpdateOne(query, update);
        }
    
        // No exeption method version
        public bool TryIsExist(User representation, string username){
            bool bSuccess = false;
            try{
                bSuccess = this.IsExist(representation, username);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return false;
            }
            return bSuccess;
        }
        public User TryGetInfo(User representation, string username){
            User tmp = null;
            try{
                tmp = this.GetInfo(representation, username);
            }
            catch (Exception e){
                Console.WriteLine(e);
                return null;
            }
            return tmp;
        }
        public bool TryAuthenticate(User representation, string username, string pass){
            bool bSuccess = false;
            try{
                bSuccess = this.Authenticate(representation, username, pass);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return false;
            }
            return bSuccess;
        }
        public bool TryAddToDatabase(User representation, 
                                string username, string rawPasswd, int money){
            try{
                this.AddToDatabase(representation, username, rawPasswd, money);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return false;
            }
            return true;
        }
        public bool TryChangeMoney(User representation,
                                string username, int additionMoney){
            try{
                this.ChangeMoney(representation, username, additionMoney);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return false;
            }
            return true;
        }
        public bool TryChangePass(User representation,
            string username, String newPass){
            try{
                this.ChangePass(representation, username, newPass);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return false;
            }
            return true;
        }
    }
}