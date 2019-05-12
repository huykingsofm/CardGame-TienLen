using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Server
{
    class User
    {
        public bool authenticate 
        {
            get;
            protected set;
        }//public only in this file
        public string username
        {
            get;
            protected set;
        }//public only in this file
        public double money
        {
            get;
            protected set;
        }//public only in this file
       
        public User(string username)
        {

        }

        class UserDatabase
        {
            static public readonly Exception USER_NOT_EXIST = new Exception("User is not exist");
            static public readonly Exception USER_EXISTED = new Exception("User is existed");
            static public readonly Exception CANT_AUTHENTICATE = new Exception("Cant authenticate");
            static public readonly Exception INVALID_USERNAME = new Exception("Invalid username");
            static public readonly Exception INVALID_PASSWORD = new Exception("Invalid password");
            static public readonly Exception INVALID_MONEY = new Exception("Money must be positive");
            private IMongoDatabase database;
            public UserDatabase(string serverpath, string dbname)
            {
                MongoClient client = new MongoClient(serverpath);
                this.database = client.GetDatabase(dbname);
            }
            private bool IsValidUserName(string username)
            {
                // Kiểm tra tên người dùng có hợp lệ hay không?
                return false;
            }
            public bool IsExist(string username)
            {
                // Kiểm tra username có tồn tại hay không?
                var collection = this.database.GetCollection<BsonDocument>("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = collection.Find(query).ToList();

                if (result.Count() == 0)
                {
                    return false;
                }
                return true;
            }

            public bool Authorize(string username, string pass)
            {
                // Xác nhận người dùng, trả về xác nhận được hay ko?
                var collection = this.database.GetCollection<BsonDocument>("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = collection.Find(query).ToList();
                if (result.Count() == 1)
                {
                    if (result[0]["password"] == Utils.HashSHA1(pass))
                        return true;
                }
                if (result.Count() == 0)
                    throw USER_NOT_EXIST;
                return false;
            }
            public User GetInfo(string username)
            {
                // Lấy thông tin *username + money* của một user, nhưng ko cấp quyền cho người đó
                // Trả về user và thông tin nếu có tồn tại, nếu không trả về null hoặc thông báo lỗi

                User usr = new User(username);

                var collection = this.database.GetCollection<BsonDocument>("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = collection.Find(query).ToList();
                if (result.Count() == 1)
                {
                    usr.money = result[0]["money"].AsDouble;
                    return usr;
                }
                throw USER_NOT_EXIST;
            }

            public User Authenticate(string username, string pass)
            {
                // Cấp quyền cho người dùng
                // Trả về User đã được cấp quyền

                if (this.Authorize(username, pass) == true)
                {
                    User usr = this.GetInfo(username);
                    usr.authenticate = true;
                    return usr;
                }

                throw CANT_AUTHENTICATE;
            }

            public void AddToDatabase(string username, string rawPasswd, double money)
            {
                // Thêm người dùng với các thông tin, trả về có thêm được hay không, hoặc throw lỗi 
                // Cần kiểm tra sự tồn tại username, độ khả dụng của mật khẩu và tiền trước khi thêm
                if (Utils.IsValidUsername(username) != 0)
                    throw INVALID_USERNAME;
                if (Utils.IsValidPassword(rawPasswd) != 0)
                    throw INVALID_PASSWORD;
                if (money < 0)
                    throw INVALID_MONEY;
                if (this.IsExist(username))
                    throw USER_EXISTED;
                string hashedPassword = Utils.HashSHA1(rawPasswd);
                BsonDocument newUser = new BsonDocument {
                    { "username", username },
                    { "password", hashedPassword },
                    { "money", money }

                };

                var collection = this.database.GetCollection<BsonDocument>("username");
                collection.InsertOne(newUser);
            }

            public bool ChangeMoney(ref User user, string additionMoney)
            {
                // Thay đổi tiền của người dùng, người dùng đã được cấp quyền mới có thể thực hiện thay đổi 
                // Chỉ có thể thay đổi bằng cách tăng (additionMoney > 0) hoặc giảm (additionMoney < 0) số tiền hiện tại
                // Sau khi thay đổi, user sẽ nhận số tiền mới
                // Trả về True/False tương ứng có thành công hay không
                if (user.authenticate == true)
                {
                    // Cần check additionMoney và currentMoney
                    this.Update(username, newMoney);
                    user.money = newMoney;
                    return true;
                }
                return false;
            }
        }
    }
}
