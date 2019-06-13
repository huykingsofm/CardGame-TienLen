using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server
{
    public class StatesCollection
    {
        public const string NO_ONE = "";
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static StatesCollection __default__ = new StatesCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public StatesCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("states");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname){
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
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
        public void Change(string username, string where, long id){
            
            lock(this){
                if (this.IsExist(username) == false){
                    BsonDocument newtoken = new BsonDocument {
                            {"username", username},
                            {"where", where},
                            {"id", id}
                        };
                        
                        this.collection.InsertOne(newtoken);
                }
                else{
                    var query = Builders<BsonDocument>.Filter.Eq("username", username);
                    var update = Builders<BsonDocument>.Update
                        .Set("where", where)
                        .Set("id", id);

                    this.collection.UpdateOne(query, update);
                }
            }
        }

        public string Where(string username, out long id){
            var query = Builders<BsonDocument>.Filter.Eq("username", username);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            id = result[0]["id"].AsInt64;
            return result[0]["where"].AsString;
        }
    }
}