using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server
{
    public class GameCollection
    {
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static GameCollection __default__ = new GameCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public GameCollection(string serverpath, string dbname)
        {
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("games");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname)
        {
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
        }
        public void Change(string server, string newserver)
        {
            // Đã tồn tại {username} trong database, chỉ cần cập nhật
            var query = Builders<BsonDocument>.Filter.Eq("server", server);
            var update = Builders<BsonDocument>.Update
                .Set("server", newserver);

            this.collection.UpdateMany(query, update);
        }
    }
}