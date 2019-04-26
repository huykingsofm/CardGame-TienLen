using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace LTMCB_GK_Backend {
    
    class UserModel {
        private String _username;
        private double _money;
        private bool _isAuthenticated;
        private Database DB;

        public String username {
            get {
                return this._username;
            }
            set {
                if(value.GetType() == typeof(String)) {
                    this._username = value;
                }
            }
        }

        public double money {
            get {
                return this._money;
            }
            set {
                if (value.GetType() == typeof(double)) {
                    this._money = value;
                }
            }
        }

        public bool isAuthenticated {
            get {
                return this._isAuthenticated;
            }
            set {
                if(value.GetType() == typeof(bool)) {
                    this._isAuthenticated = true;
                }
            }
        }

        public UserModel() {
            this._username = "";
            this._money = 0.0;
            this.isAuthenticated = false;
        }
        public UserModel(String mongodbPath, String database) {
            this.DB = new Database(mongodbPath, database);
            this._username = "";
            this._money = 0.0;
            this.isAuthenticated = false;
        }
        public UserModel(Database db) {
            this.DB = db;
            this._username = "";
            this._money = 0.0;
            this.isAuthenticated = false;
        }

        //Secure & Encrypte Data
        public static String HashSHA1(String value) {
            var sha1 = SHA1.Create();
            var inputBytes = Encoding.ASCII.GetBytes(value);
            var hash = sha1.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public override String ToString() {
            return this.username + "&&" + this._money;
        }

        public bool isExisted(String username) {
            var col = this.DB.GetCollection("users");

            List<BsonDocument> docs = col.Find(new BsonDocument()).ToList();
            foreach(BsonDocument doc in docs) {
                if(doc["username"].AsString == username) {
                    return true; //This user existed
                }
            }

            return false; //user not existed
        }

        public bool isValidInput(String username, String password) {
            if(username.Length < 4 || 
               username.Length > 20||
               password.Length < 5 ||
               password.Length > 20) {

                return false;
            }

            return true;
        }

        public UserModel FindUser(String username) {
            try {
                UserModel user = new UserModel(this.DB);

                var col = this.DB.GetCollection("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = col.Find(query).ToList();

                if(result.Count() == 0) {
                    throw new Exception("User not found");
                }

                user.username = result[0]["username"].AsString;
                user.money = result[0]["money"].AsDouble;

                return user;
            } catch(Exception e) {
                Console.WriteLine(e.Message);

                return null;
            }
        }

        private String findPassword(String username) {
            try {
                var col = this.DB.GetCollection("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = col.Find(query).ToList();

                if (result.Count() == 0) {
                    throw new Exception("User not found");
                }

                return result[0]["password"].AsString;
            } catch (Exception e) {
                Console.WriteLine(e.Message);

                return null;
            }
        }

        private double findMoney(String username) {
            try {
                var col = this.DB.GetCollection("users");
                var query = Builders<BsonDocument>.Filter.Eq("username", username);

                List<BsonDocument> result = col.Find(query).ToList();

                if (result.Count() == 0) {
                    throw new Exception("User not found");
                }

                return result[0]["money"].AsDouble;
            } catch (Exception e) {
                Console.WriteLine(e.Message);

                return -1.0; //For failure
            }
        }

        public bool AuthorizeUser(String username, String password) {
            try {
                if(!this.isAuthenticated) {
                    throw new Exception("User not authorized");
                }

                return true;
            } catch(Exception e) {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public UserModel Authenticate(String username, String password) {
            try {
                var database = this.DB.GetDatabase();
                IMongoCollection<BsonDocument> users =
                   database.GetCollection<BsonDocument>("users");

                UserModel user = this.FindUser(username);
                if (user == null) {
                    throw new Exception("User not found");
                } else if (this.findPassword(username) != HashSHA1(password)) {
                    throw new Exception("Password is incorrect");
                }

                this.isAuthenticated = true;
                this.username = username;
                this.money = this.findMoney(username);

                return this;
            } catch (Exception e) {
                Console.WriteLine(e.Message);

                return null;
            }
        }

        public bool AddUser(String username, String password) {
            try {
                var database = this.DB.GetDatabase();
                IMongoCollection<BsonDocument> users =
                   database.GetCollection<BsonDocument>("users");

                //check if user existed
                if(this.isExisted(username)) {
                    throw new Exception("User existed!");
                }
                if(!this.isValidInput(username, password)) {
                    throw new Exception("Invalid input!");
                }
                
                String hashedPassword = HashSHA1(password);

                this.username = username;
                this.money = 0.5;

                BsonDocument newUser = new BsonDocument {
                    { "username", username },
                    { "password", hashedPassword },
                    { "money", 0.5 }

                };

                users.InsertOne(newUser);

                return true;
            } catch (Exception e) {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public bool ChangePassword(String username, String oldPass, String newPass) {
            try {
                var database = this.DB.GetDatabase();
                IMongoCollection<BsonDocument> users =
                   database.GetCollection<BsonDocument>("users");

                UserModel user = this.FindUser(username);
                if(user == null) {
                    throw new Exception("User not found");
                } else if(this.findPassword(username) != oldPass) {
                    throw new Exception("Password is incorrect");
                }

                var query = Builders<BsonDocument>.Filter.Eq("username", username);
                var update = Builders<BsonDocument>.Update
                    .Set("password", HashSHA1(newPass));
                users.UpdateOne(query, update);

                return true;
            } catch(Exception e) {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public bool UpdateMoney(double newMoney) {
            try {
                if(!this.isAuthenticated) {
                    throw new Exception("User not authorized");
                }

                var database = this.DB.GetDatabase();
                IMongoCollection<BsonDocument> users =
                   database.GetCollection<BsonDocument>("users");

                var query = Builders<BsonDocument>.Filter.Eq("username", this.username);
                var update = Builders<BsonDocument>.Update
                    .Set("money", (money + newMoney) >= 0 ? (money + newMoney) : 0.0);
                users.UpdateOne(query, update);

                this.money = (this.money + newMoney) >= 0 ?
                    (this.money + newMoney) : 0.0; 

                return true;

            } catch(Exception e) {
                Console.WriteLine(e.Message);

                return false;
            }
        }
    }
}
