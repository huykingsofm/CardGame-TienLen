using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Json;

namespace Server
{
    public class WorkingCollection
    {
        public const string PLAYING = "playing";
        public const string NONE = "none";
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static WorkingCollection __default__ = new WorkingCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public WorkingCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("working");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname){
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
        }
        public bool IsPlaying(string username){
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                string status = result[0]["status"].AsString;
                if (status == WorkingCollection.PLAYING)
                    return true; 
            }
            return false;
        }
        protected bool IsExist(string username){
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return true;

            else if (result.Count() == 0)
                return false;

            throw new Exception("Error in database");
        }
        public void Change(string server, string status){
            lock(this){
                
                
                // Đã tồn tại {username} trong database, chỉ cần cập nhật
                var query = Builders<BsonDocument>.Filter.Eq("where", server);
                var update = Builders<BsonDocument>.Update
                    .Set("status", status);

                this.collection.UpdateMany(query, update);
            }
        }
        public string Where(string username){
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return result[0]["where"].AsString;

            else if (result.Count() == 0)
                throw new Exception("User is not exist in Working Collection");

            throw new Exception("Error in database");
        }
        public string[] Who(string where){
            var query = Builders<BsonDocument>.Filter.Eq("where", where);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 0)
                throw new Exception("This place is not exist in Working Collection");

            List<string> users = new List<string>();

            foreach(var res in result)
                users.Add(res["username"].AsString);

            return users.ToArray();
        }
    }
}