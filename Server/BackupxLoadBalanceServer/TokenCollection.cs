using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server
{
    public class TokenCollection
    {
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static TokenCollection __default__ = new TokenCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public TokenCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("tokens");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname){
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
        }

        public string GetToken(string username){
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                string token = result[0]["token"].AsString;
                return token; 
            }
            throw new Exception("User is not exist or no-token for this user");
        }

        public string GetUsername(string token){
            var query = Builders<BsonDocument>.Filter.Eq("token", token);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                string username = result[0]["username"].AsString;
                return username; 
            }
            throw new Exception("Token is not exist");
        }

        public void Add(string username, string token){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            lock(this){
                try{
                    this.GetToken(username);
                }
                catch{
                    // Không tồn tại cặp {token,username} trong database
                    BsonDocument newtoken = new BsonDocument {
                        { "username", username },
                        { "token", token}
                    };
                    
                    this.collection.InsertOne(newtoken);
                    return;
                }
                
                // Đã tồn tại cặp {token, username} trong database, chỉ cần cập nhật
                var query = Builders<BsonDocument>.Filter.Eq("username", username);
                var update = Builders<BsonDocument>.Update
                    .Set("token", token);

                this.collection.UpdateOne(query, update);
            }
        }
    }
}