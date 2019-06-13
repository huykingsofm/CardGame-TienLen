using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Server
{
    public class RoomCollection
    {
        public const string NO_ONE = "";
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static RoomCollection __default__ = new RoomCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public RoomCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("room");
        }
        public void SetUpNewCollection(string serverpath, string dbname, string collectionname){
            /*
            # Mục đích : Thiết lập một Collection mới thay cho collection mặc định
            */
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>(collectionname);
        }
        protected bool IsExist(long id){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", id);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return true;

            else if (result.Count() == 0)
                return false;

            throw new Exception("Error in database");
        }
        public void Add(long idlobby, long idroom, int betmoney){
            
            lock(this){
                if (this.IsExist(idroom) == false){
                    BsonDocument newtoken = new BsonDocument {
                            { "idlobby", idlobby},
                            { "idroom",  idroom },
                            { "betmoney", betmoney},
                            { "status", Room.ROOM_WAITING},
                            { "host", -1},
                            { "lastwinner", -1},
                            { "player1", ""},
                            { "player2", ""},
                            { "player3", ""},
                            { "player4", ""},
                            { "status1", Room.NOT_IN_ROOM},
                            { "status2", Room.NOT_IN_ROOM},
                            { "status3", Room.NOT_IN_ROOM},
                            { "status4", Room.NOT_IN_ROOM},
                            { "game", false}
                        };
                        
                        this.collection.InsertOne(newtoken);
                }
                else{
                    throw new Exception("Room do exist before");
                }
            }
        }
        public void SetGame(long idroom, bool value){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("One error in Room Collection");

            var update = Builders<BsonDocument>.Update
                .Set("game", value);
            this.collection.UpdateOne(query, update);
        }
        public bool GameExist(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("One error in Room Collection");
            
            return result[0]["game"].AsBoolean;
        }
        public int HowMany(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();


            string[] s = new string[]{"status1", "status2", "status3", "status4"};
            int count = 4;
            for (int i = 0; i < 4; i++)
                if (result[0][s[i]].AsInt32 == Room.NOT_IN_ROOM)
                    count -= 1;
            
            return count;
        }
        public int AddUserToRoom(long idroom, string username){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            try{
                // Đặt người chơi vào vị trí trước đó của họ nếu họ đã từng afk mà game vẫn còn
                int index = GameCollection.__default__.Where(idroom, username);
                if (index == -1)
                    throw new Exception();

                string[] p = new string[]{"player1", "player2", "player3", "player4"};
                string[] s = new string[]{"status1", "status2", "status3", "status4"};

                var update = Builders<BsonDocument>.Update
                            .Set(p[index], username)
                            .Set(s[index], Room.PLAYING);  
                
                this.collection.UpdateOne(query, update);
                StatesCollection.__default__.Change(username, "game", idroom);  
                return index;
            }
            catch{
                int[] playerstatus = this.GetAllPlayerStatus(idroom);
                int host = result[0]["host"].AsInt32;

                int availableslot = -1;
                for (int i = 0; i < playerstatus.Count(); i++)
                    if (playerstatus[i] == Room.NOT_IN_ROOM){
                        availableslot = i;
                        break;
                    }

                // Tại đây, cần kiểm tra người chơi có phải đã afk ko, nếu phải
                // khôi phục lại vị trí đó cho người chơi.
                // Cần hoàn thành gameCollection...

                if (availableslot == -1)
                    throw new Exception("Room is full");     

                string[] p = new string[]{"player1", "player2", "player3", "player4"};
                string[] s = new string[]{"status1", "status2", "status3", "status4"};

                var update = Builders<BsonDocument>.Update
                            .Set(p[availableslot], username)
                            .Set(s[availableslot], Room.NOT_READY);  
                
                if (host == -1)
                    update = update.Set("host", availableslot);
                
                this.collection.UpdateOne(query, update);   
                StatesCollection.__default__.Change(username, "room", idroom);
                return availableslot;
            }
        }
        public int RemoveUserFromRoom(long idroom, string username){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string[] usernames = this.GetAllPlayerNames(idroom);
            int host = result[0]["host"].AsInt32;
            int lastwinner = result[0]["lastwinner"].AsInt32;

            int index = -1;
            for (int i = 0; i < usernames.Count(); i++)
                if (usernames[i] == username)
                    index = i;
            
            if (index == -1)
                throw new Exception("Player do not exist in room");

            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            
            int newstatus = Room.NOT_IN_ROOM;
            if (this.GameExist(idroom))
                newstatus = Room.AFK;

            host = host == index ? -1 : host;
            lastwinner = lastwinner == index ? -1 : lastwinner;

            if (host == -1)
                for (int add = 1; add < 4; add++){
                    int i = (index + add) % 4;
                    if (result[0][s[i]].AsInt32 != Room.NOT_IN_ROOM &&
                        result[0][s[i]].AsInt32 != Room.AFK &&
                        result[0][s[i]].AsInt32 != Room.AI){
                        host = i;
                        break;
                    }
                }
            
            var update = Builders<BsonDocument>.Update
                .Set(p[index], "")
                .Set(s[index], newstatus)
                .Set("host", host)
                .Set("lastwinner", lastwinner);  
                        
            
            this.collection.UpdateOne(query, update);  
            return index;
        }
        public void Ready(long idroom, string username){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            int status = result[0]["status"].AsInt32;
            int host = result[0]["host"].AsInt32;
            
            if (status == Room.PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            int index = -1;
            for (int i = 0; i < 4; i++)
                if (result[0][p[i]].AsString == username){
                    index = i;
                    break;
                }

            if (host == index)
                throw new Exception("You are the host. It is unnecessary to ready");

            if (index == -1)
                throw new Exception("Player do not exist in room");

            int yourstatus = result[0][s[index]].AsInt32;
            if (yourstatus != Room.NOT_READY)
                throw new Exception("Player cannot ready now");
        
            var update = Builders<BsonDocument>.Update
                .Set(s[index], Room.READY);
            
            this.collection.UpdateOne(query, update);
        }
        public void UnReady(long idroom, string username){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            int status = result[0]["status"].AsInt32;
            int host = result[0]["host"].AsInt32;
            
            if (status == Room.PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            int index = -1;
            for (int i = 0; i < 4; i++)
                if (result[0][p[i]].AsString == username){
                    index = i;
                    break;
                }

            if (index == -1)
                throw new Exception("Player do not exist in room");

            int yourstatus = result[0][s[index]].AsInt32;
            if (yourstatus != Room.READY)
                throw new Exception("Player cannot unready now");
        
            var update = Builders<BsonDocument>.Update
                .Set(s[index], Room.NOT_READY);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetAI(long idroom, int index){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            int status = result[0]["status"].AsInt32;
            int slotstatus = result[0][s[index]].AsInt32;
            
            if (status == Room.PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            if (slotstatus == Room.AI)
                throw new Exception("This position has already been a AI");

            if (slotstatus != Room.NOT_IN_ROOM)
                throw new Exception("This position was hold by another player");

            var update = Builders<BsonDocument>.Update
                .Set(s[index], Room.AI);
            
            this.collection.UpdateOne(query, update);
        }
        public void RemoveAI(long idroom, int index){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            int status = result[0]["status"].AsInt32;
            int slotstatus = result[0][s[index]].AsInt32;
            
            if (status == Room.PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            if (slotstatus == Room.NOT_IN_ROOM)
                throw new Exception("This status is a empty slot");

            if (slotstatus != Room.AI)
                throw new Exception("This position is not a AI slot");

            var update = Builders<BsonDocument>.Update
                .Set(s[index], Room.NOT_IN_ROOM);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetRoomStatus(long idroom, int status){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            var update = Builders<BsonDocument>.Update
                .Set("status", status);
            
            this.collection.UpdateOne(query, update);  
        }
        public void SetPlayerStatus(long idroom, int index, int status){
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            var update = Builders<BsonDocument>.Update
                .Set(s[index], status);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetPlayerStatus(long idroom, string username, int status){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            int index = -1;
            for (int i = 0; i < 4; i++)
                if (result[0][p[i]].AsString == username){
                    index = i;
                    break;
                }

            if (index == -1)
                throw new Exception("Player do not exist in room");

            var update = Builders<BsonDocument>.Update
                .Set("status", status);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetLastWinner(long idroom, int value){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            var update = Builders<BsonDocument>.Update
                .Set("lastwinner", value);
            
            this.collection.UpdateOne(query, update);  
        }
        public void SetBetMoney(long idroom, int betmoney){
            if (this.GetRoomStatus(idroom) == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing, dont set bet money");
        
            if (betmoney < 0)
                throw new Exception("Bet money must be positive number");
            
            if (betmoney > 1e6)
                throw new Exception("Bet money is too big");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            var update = Builders<BsonDocument>.Update
                .Set("betmoney", betmoney);
            
            this.collection.UpdateOne(query, update);
        }
        public int GetBetMoney(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["betmoney"].AsInt32;
        }
        public int GetLastWinner(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["lastwinner"].AsInt32;
        }
        public string GetLobbyInfo(long idlobby){
            var query = Builders<BsonDocument>.Filter.Eq("idlobby", idlobby);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 0)
                throw new Exception("No lobby like this in database");

            int n = result.Count();

            string[] rooms = new string[30];

            for (int i = 0; i < n; i++){
                long idroom = result[i]["idroom"].AsInt64;
                rooms[idroom * 3] = this.HowMany(result[i]["idroom"].AsInt64).ToString();
                rooms[idroom * 3 + 1] = result[i]["betmoney"].AsInt32.ToString();
                rooms[idroom * 3 + 2] = result[i]["status"].AsInt32.ToString();
            }

            return "{0},{1}".Format(n, String.Join(",", rooms));            
        }
        public string GetRoomInfo(long idroom, int view){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1){
                int bm = result[0]["betmoney"].AsInt32;
                int h = (4 + result[0]["host"].AsInt32 - view) % 4;
                string[] users = new string[12];

                string[] p = new string[]{"player1", "player2", "player3", "player4"};
                string[] s = new string[]{"status1", "status2", "status3", "status4"};

                for (int add = 0; add < 4; add++){
                    int i = (view + add) % 4;
                    if (result[0][s[i]].AsInt32 == 0){
                        users[add * 3] = "none";
                        users[add * 3 + 1] = "0";
                        users[add * 3 + 2] = "0";
                    }
                    else{
                        if (result[0][p[i]].AsString != ""){
                            users[add * 3] = result[0][p[i]].AsString;
                            users[add * 3 + 1] = UserCollection.__default__
                                .GetInfo(User.__administrator__, users[add * 3]).money.ToString();
                        }
                        else{
                            users[add * 3] = "someone";
                            users[add * 3 + 1] = "0";
                        }

                        users[add * 3 + 2] = result[0][s[i]].AsInt32.ToString();
                    }
                }

                return "{0},{1},{2}".Format(bm, h, String.Join(",", users));
            }
            else if (result.Count() == 0)
                throw new Exception("No lobby like this in database");

            throw new Exception("Error in database");
        }
        public int GetHost(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["host"].AsInt32;
        }
        public int GetRoomStatus(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["status"].AsInt32;
        }
        public string[] GetAllPlayerNames(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();


            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] playernames = new string[4];

            for (int i = 0; i < 4; i++)
                if (result[0][p[i]].AsString == "")
                    playernames[i] = null;
                else
                    playernames[i] = result[0][p[i]].AsString;
            
            return playernames;
        }
        public int GetPlayerStatus(long idroom, string username){
            if ( UserCollection.__default__.IsExist(
                    User.__administrator__,
                    username
                ) == false )
                throw new Exception("Username is not exist in Database");

            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            int index = -1;
            for (int i = 0; i < 4; i++)
                if (result[0][p[i]].AsString == username){
                    index = i;
                    break;
                }

            if (index == -1)
                throw new Exception("Player do not exist in room");

            return result[0][s[index]].AsInt32;
        }
        public int GetPlayerStatus(long idroom, int index){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0][s[index]].AsInt32;
        }
        public int[] GetAllPlayerStatus(long idroom){
            var query = Builders<BsonDocument>.Filter.Eq("idroom", idroom);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            string[] s = new string[]{"status1", "status2", "status3", "status4"};

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            List<int> ret = new List<int>();

            foreach(var status in s)
                ret.Add(result[0][status].AsInt32);

            return ret.ToArray();
        }
    }
}