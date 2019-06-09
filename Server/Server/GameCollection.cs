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
        public const string NO_ONE = "";
        const string default_serverpath = "mongodb://localhost:27017";
        const string default_dbname = "CardGame-TienLen";
        public static GameCollection __default__ = new GameCollection(default_serverpath, default_dbname);
        private IMongoCollection<BsonDocument> collection = null;
        public GameCollection(string serverpath, string dbname){
            MongoClient client = new MongoClient(serverpath);
            IMongoDatabase database = client.GetDatabase(dbname);
            this.collection = database.GetCollection<BsonDocument>("games");
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
            var query = Builders<BsonDocument>.Filter.Eq("idgame", id);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
                return true;

            else if (result.Count() == 0)
                return false;

            throw new Exception("Error in database");
        }
        public void Add(long id, string[] playernames, CardSet[] cards, int Starter, Card smallestcard){
            if (playernames.Count() != 4)
                throw new Exception("Playername array is not in format");
            
            for (int i = 0; i < playernames.Count(); i++){
                if (playernames[i] == null){
                    playernames[i] = "";
                    continue;
                }
                
                if (playernames[i] != "" && UserCollection.__default__.IsExist(
                        User.__administrator__,
                        playernames[i]
                    ) == false )
                    throw new Exception("Username is not exist in Database");
            }
            lock(this){
                if (this.IsExist(id))
                    throw new Exception("Game existed in database");
                
                string[] cardcontents = new string[4];
                for (int i =0; i < 4; i++)
                    cardcontents[i] = cards[i] == null ? "" : cards[i].ToString(sum:false);

                BsonDocument newgame = new BsonDocument {
                        { "idgame", id },
                        { "player1", playernames[0]},
                        { "player2", playernames[1]},
                        { "player3", playernames[2]},
                        { "player4", playernames[3]}, 
                        { "card1", cardcontents[0]},
                        { "card2", cardcontents[1]},
                        { "card3", cardcontents[2]},
                        { "card4", cardcontents[3]},
                        { "whoturn", Starter},
                        { "lastmove", ""},
                        { "lastplayer", Starter},
                        { "onboardsets", "0"},
                        { "specialchain", 0},
                        { "endgamesignal", false},
                        { "smallestcard", smallestcard.ToString()},
                        { "time", ""},
                        { "timerstop", false},
                        { "playingcard", ""},
                        { "timeout", false},
                        { "server", Program.socket}
                    };
                    
                    this.collection.InsertOne(newgame);
            }
        }
        public string[] Get(long id){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", id);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() == 1)
            {
                string[] key = new string[]{"player1", "player2", "player3", "player4"};
                string[] ret = new string[4];
                for (int i = 0; i < ret.Count(); i++)
                    if (result[0][key[i]].AsString != null)
                        ret[i] = result[0][key[i]].AsString;
                return ret; 
            }
            else if (result.Count() == 0)
                return null;

            throw new Exception("Error in database");
        }
        public int Where(long id, string username){
            string[] ret = this.Get(id);
            for (int i = 0; i < ret.Count(); i++)
                if (ret[i] == username)
                    return i;
            return -1;
        }
        public void Remove(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            this.collection.DeleteOneAsync(query);
        }
        public Move GetLastMove(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string lastmovestr = result[0]["lastmove"].AsString;
            if (lastmovestr == "")
                return null;

            string[] cards = lastmovestr.Split(',');
            CardSet cardset  = CardSet.Create(cards, "list");

            return Move.Create(cardset);
        }
        public int GetWhoTurn(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["whoturn"].AsInt32;
        }
        public CardSet GetCardSet(long idgame, int index){
            string[] c = new string[]{"card1", "card2", "card3", "card4"};

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string content = result[0][c[index]].AsString;
            if (content == "")
                return null;

            CardSet card = CardSet.Create(content.Split(','), "list");
            return card;
        }
        public CardSet GetInTurnCardSet(long idgame){
            string[] c = new string[]{"card1", "card2", "card3", "card4"};

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            int index = result[0]["whoturn"].AsInt32;
            string content = result[0][c[index]].AsString;
            if (content == "")
                return null;

            CardSet card = CardSet.Create(content.Split(','), "list");
            return card;
        }
        public bool GetEndGameSignal(long idgame){
         var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["endgamesignal"].AsBoolean;
        }
        public Card GetSmallestCard(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            string content = result[0]["smallestcard"].AsString;
            if (content == "")
                return null;

            Card card = Card.Create(content);
            return card;
        }
        public int GetSpecialChain(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["specialchain"].AsInt32;
        }
        public string GetTime(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["time"].AsString;
        }
        public bool GetTimerStop(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["timerstop"].AsBoolean;
        }
        public string GetServer(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["server"].AsString;
        }
        public int[] GetAllPlayerStatus(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            int[] playerstatus = new int[4];

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            for (int i = 1; i <= 4; i++)
                playerstatus[i - 1] = result[0]["status" + i].AsInt32;

            return playerstatus;
        }
        public int GetLastPlayer(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            return result[0]["lastplayer"].AsInt32;
        }
        public string[] GetPlayerNames(long idgame){
            string[] p = new string[]{"player1", "player2", "player3", "player4"};
            string[] playernames = new string[4];

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            for (int i = 0; i < 4; i++)
                if (result[0][p[i]] == "")
                    playernames[i] = null;
                else
                    playernames[i] = result[0][p[i]].AsString;
                
            return playernames;
        }
        public string GetPlayerName(long idgame, int index){
            string[] p = new string[]{"player1", "player2", "player3", "player4"};
           
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");
                
            return result[0][p[index]].AsString;
        }
        public CardSet[] GetAllCardSets(long idgame){
            string[] c = new string[]{"card1", "card2", "card3", "card4"};
            CardSet[] cardsets = new CardSet[4];

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");

            for (int i = 0; i < 4; i++){
                string content = result[0][c[i]].AsString;
                if (content == "")
                    cardsets[i] = null;
                else
                    cardsets[i] = CardSet.Create(content.Split(','), "list");
            }
            return cardsets;
        }
        public List<CardSet> GetOnBoardSets(long idgame){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            if (result.Count() != 1)
                throw new Exception("idroom is incorrect");
            
            List<CardSet> onboardset = new List<CardSet>();
            string content = result[0]["onboardsets"].AsString;
            string[] args = content.Split(',');
            int start = 1;
            for (int i = 0; i < Int32.Parse(args[0]); i++){
                string[] cardstr = args.Take(start + 1, Int32.Parse(args[start]));
                CardSet tmp = CardSet.Create(cardstr, "list");
                start += Int32.Parse(args[start]) + 1;
                onboardset.Add(tmp);
            }
            return onboardset;
        }
        public string GetGameInfo(long idgame, int view){
            if (view < 0 || view > 4)
                throw new Exception("User[{0}] is not exist in game".Format(view));

            List<string> arr = new List<string>();

            CardSet[] cards = this.GetAllCardSets(idgame);

            // Thông tin của người chơi thứ index
            if (cards[view] == null)
                arr.Add("0");
            else
                arr.Add(cards[view].ToString());

            int onturn = 0;
            for (int add = 1; add < 4; add++){
                int i = (view + add) % 4;
                if (cards[i] == null)
                    arr.Add("0");
                else
                    arr.Add(cards[i].Count().ToString());
                
                if (i == this.GetWhoTurn(idgame))
                    onturn = add;
            }

            arr.Add(onturn.ToString());
            
            int pass = this.GetWhoTurn(idgame) == view ? 0 : -1;
            pass = (pass == 0) && this.GetWhoTurn(idgame) == this.GetLastPlayer(idgame) ? 1 : pass;
            arr.Add(pass.ToString());

            string ret = String.Join(",", arr);
            return ret;
        }
        public void SetSmallestCard(long idgame, Card card){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            
            string content = card != null ? card.ToString() : "";
            var update = Builders<BsonDocument>.Update
                .Set("smallestcard", content);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetInTurnCardSet(long idgame, CardSet newcardset){
            string[] c = new string[]{"card1", "card2", "card3", "card4"};

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            List<BsonDocument> result = this.collection.Find(query).ToList();

            int whoturn = result[0]["whoturn"].AsInt32;

            string content = newcardset == null ? "" : newcardset.ToString(sum:false);

            var update = Builders<BsonDocument>.Update
                .Set(c[whoturn], content);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetEndGameSignal(long idgame, bool status){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("endgamesignal", status);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetSpecialChain(long idgame, int value){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("specialchain", value);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetLastMove(long idgame, Move move){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);

            string content = move == null ? "" : move.GetMoveSet().ToString(sum:false);
            var update = Builders<BsonDocument>.Update
                .Set("lastmove", content);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetLastPlayer(long idgame, int value){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("lastplayer", value);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetOnBoardSets(long idgame, List<CardSet> onboardsets){
            List<string> content = new List<string>();
            content.Add(onboardsets.Count().ToString());

            for (int i = 0; i < onboardsets.Count(); i++)
                content.Add(onboardsets[i].ToString(sum:true));

            string newcontent = String.Join(',', content);

            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("onboardsets", newcontent);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetWhoTurn(long idgame, int newwhoturn){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("whoturn", newwhoturn);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetTime(long idgame, string time){
            var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("time", time);
            
            this.collection.UpdateOne(query, update);
        }
        public void SetTimerStop(long idgame, bool stop){
        var query = Builders<BsonDocument>.Filter.Eq("idgame", idgame);
            var update = Builders<BsonDocument>.Update
                .Set("timerstop", stop);
            
            this.collection.UpdateOne(query, update);
        }
        public void AddOnBoardSets(long idgame, CardSet newcardset){
            List<CardSet> onboardset = this.GetOnBoardSets(idgame);
            onboardset.Add(newcardset);

            this.SetOnBoardSets(idgame, onboardset);
        }
    }
}