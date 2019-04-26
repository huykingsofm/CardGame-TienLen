using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LTMCB_GK_Backend {
    class Database {
        private String MongoDBPath;
        private IMongoDatabase database;
        private IMongoClient client;

        public Database() { }
        public Database(String mongodbPath, String db) {
            this.MongoDBPath = mongodbPath;
            this.client = new MongoClient(mongodbPath);
            this.database = this.client.GetDatabase(db);
        }

        public IMongoDatabase GetDatabase() {
            return this.database;
        }

        public IMongoCollection<BsonDocument> GetCollection(String collectionName) {
            return this.database.GetCollection<BsonDocument>(collectionName);
        }
    }
}
