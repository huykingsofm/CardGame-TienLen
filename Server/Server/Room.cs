using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Room : Thing{
        /*
         * Mục đích : Một lớp hỗ trợ xử lý client khi đã vào phòng.
         * Thuộc tính : 
         *      + host          : Chỉ số của chủ phòng.
         *      + BetMoney      : Tiền cược.
         *      + clients[]     : Các client đang trong phòng.
         *      + game          : Đại diện cho game được chơi trong phòng.
         *      + lobby         : Lưu trữ lobby đang kết nối với room.
         *      + PlayerStatus  : Lưu trữ trạng thái của các client trong phòng.
         * Khởi tạo : 
         *      + Room(Lobby)   : Không thể truy cập trực tiếp.
         *      + Create(Lobby) : Tạo ra một Room với Lobby chỉ định.
         * Phương thức :
         *      + Add(Client)    : Thêm một client vào phòng.
         *      + Remove(Client) : Loại bỏ client ra khỏi phòng.
         *      + Destroy()      : Đẩy các client ra khỏi phòng.
         *      + StartGame()    : Bắt đầu một game.
         *      + GeneralInfo()  : Lấy thông tin tổng quát của phòng(số lượng người + tiền cược).
         *      + Ready(Client)  : Thực hiện thay đổi trạng thái của client thành ready.
         *      + UnReady(Client): Thực hiện thay đổi trang thái của client thành unready.
         *      + ToString(int)  : Trả về thông tin của phòng dưới góc nhìn của một người chơi.
         */
        public override string Name => "Room";
        private Game game;
        private Lobby lobby;
        /* public int host{get; private set;}
        public int lastwinner{get; private set;}
        public int BetMoney{get; private set;}
        private Client[] clients;
        private int[] PlayerStatus; /*  
                                     *    0 - không trong phòng
                                     *    1 - có trong phòng nhưng chưa sẵn sàng
                                     *    2 - có trong phòng và đã sẵn sàng
                                     *    3 - có trong phòng và đang chơi game
                                     *    4 - vị trí tự động chơi game vì người chơi đã thoát đột ngột
                                     *    5 - sử dụng AI cho vị trí này 
                                     
        private int RoomStatus;     /*
                                     *    Phòng đang đợi (WAITING)
                                     *    Phòng đang chơi (PLAYING)
                                     
                                    */
        public int id{get; private set;}
        public const int NOT_IN_ROOM = 0;
        public const int NOT_READY = 1;
        public const int READY = 2;
        public const int PLAYING = 3;
        public const int AFK = 4;
        public const int AI = 5;
        public const int ROOM_WAITING = 0;
        public const int ROOM_PLAYING = 1;
        protected Room(Lobby lobby, int id){
            if (lobby == null)
                throw new Exception("Lobby can not be a null instances");

            this.lobby = lobby;
            this.id = id;
            try{
                RoomCollection.__default__.Add(
                    idlobby     : lobby.id, 
                    idroom      : this.id, 
                    betmoney    : (id + 1) * 5
                );
            }
            catch{
                // do nothing
            }
        }
        public static Room Create(Lobby lobby, int id){
            Room room = null;
            
            try{
                room = new Room(lobby, id);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }
            
            return room;
        }
        public int Add(string playername){
           return RoomCollection.__default__.AddUserToRoom(this.id, playername);
        }
        public int Remove(string playername){
            return RoomCollection.__default__.RemoveUserFromRoom(this.id, playername);
        }
        public void Ready(string playername){
            RoomCollection.__default__.Ready(this.id, playername);
        }
        public void UnReady(string playername){
            RoomCollection.__default__.UnReady(this.id, playername);
        }
        public void SetAI(int index){
            RoomCollection.__default__.SetAI(this.id, index);
        }
        public void RemoveAI(int index){
            RoomCollection.__default__.RemoveAI(this.id, index);
        }
        public void SetBetMoney(int betmoney){
            RoomCollection.__default__.SetBetMoney(this.id, betmoney);
        }
        public void StartGame(){
            if (this.GetRoomStatus() == Room.ROOM_PLAYING)
                throw new Exception("Room are playing");
                
            int[] PlayerStatus = this.GetAllPlayerStatus();

            if (PlayerStatus.CountDiff(Room.NOT_IN_ROOM) < 2)
                throw new Exception("It need at least 2 player to start game");

            int ready = PlayerStatus.Count(element:Room.READY) + 1; // Chủ phòng không cần ready
            int ai = PlayerStatus.Count(element:Room.AI);

            int inroom = PlayerStatus.CountDiff(element:Room.NOT_IN_ROOM);

            if (ready + ai < inroom)
                throw new Exception("Someone hasn't been ready yet");

            try{
                Game.CreateInCollection(this.id, this.GetAllPlayerNames(), PlayerStatus, this.GetLastWinner());
            }
            catch(Exception e){
                this.WriteLine(e.Message);
                return;
            }

            string[] playernames = GameCollection.__default__.GetPlayerNames(this.id);
            for (int i = 0; i<playernames.Count(); i++)
                if (playernames[i] != null)
                    StatesCollection.__default__.Change(playernames[i], "game", this.id);

            RoomCollection.__default__.SetRoomStatus(this.id, Room.ROOM_PLAYING);
        }
        public Game CreateGame(){
            this.game = new Game(this.id);
            return this.game;
        }
        public void Refresh(){
            string[] playernames = this.GetAllPlayerNames(); 
            int[] playerstatus = this.GetAllPlayerStatus();
            for (int i = 0; i < 4; i++){
                if (playernames[i] == null)
                    this.SetPlayerStatus(i, Room.NOT_IN_ROOM);
            
                if (this.GetLastWinner() == i && playerstatus[i] == Room.NOT_IN_ROOM)
                    this.SetLastWinner(-1);
            }
        }
        public void StopGame(int winner){
            if (this.GetRoomStatus() == Room.ROOM_WAITING){
                this.WriteLine("Room are waiting, no game to stop");
                return;
            }
            string[] playernames = this.GetAllPlayerNames();
            for (int i = 0; i < 4; i++)
                if (playernames[i] != null)
                    this.SetPlayerStatus(i, Room.NOT_READY);
                
            this.game = null;
            
            this.SetRoomStatus(Room.ROOM_WAITING);
            this.Refresh();

            if (playernames[winner] != null)
                this.SetLastWinner(winner);
            else
                this.SetLastWinner(-1);
        
            for (int i = 0; i<playernames.Count(); i++)
                if (playernames[i] != null)
                    StatesCollection.__default__.Change(playernames[i], "room", this.id);

        }
        public void Destroy(){
            string[] playernames = this.GetAllPlayerNames();
            foreach(var playername in playernames)
                if (playername != null){
                    this.Remove(playername);
                    this.lobby.Add(playername);
                }
        }
        public string ToString(int index){
            return RoomCollection.__default__.GetRoomInfo(this.id, index);
        }
        public void SetLastWinner(int value){
            RoomCollection.__default__.SetLastWinner(this.id, value);
        }
        public void SetPlayerStatus(int index, int value){
            RoomCollection.__default__.SetPlayerStatus(this.id, index, value);
        }
        public void SetRoomStatus(int value){
            RoomCollection.__default__.SetRoomStatus(this.id, value);
        }
        public int GetHost(){
            return RoomCollection.__default__.GetHost(this.id);
        }
        public int GetRoomStatus(){
            return RoomCollection.__default__.GetRoomStatus(this.id);
        }
        public int[] GetAllPlayerStatus(){
            return RoomCollection.__default__.GetAllPlayerStatus(this.id);
        }
        public bool GameExist(){
            return RoomCollection.__default__.GameExist(this.id);
        }
        public string[] GetAllPlayerNames(){
            return RoomCollection.__default__.GetAllPlayerNames(this.id);
        }
        public int GetLastWinner(){
            return RoomCollection.__default__.GetLastWinner(this.id);
        }
        public int GetBetMoney(){
            return RoomCollection.__default__.GetBetMoney(this.id);
        }
        public int GetPlayerStatus(int index){
            return RoomCollection.__default__.GetPlayerStatus(this.id, index);
        }
    }
}