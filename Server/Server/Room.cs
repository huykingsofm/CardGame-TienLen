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
        public int host{get; private set;}
        public int lastwinner{get; private set;}
        public int BetMoney{get; private set;}
        private Client[] clients;
        private Game game;
        private Lobby lobby;
        private int[] PlayerStatus; /*  
                                     *    0 - không trong phòng
                                     *    1 - có trong phòng nhưng chưa sẵn sàng
                                     *    2 - có trong phòng và đã sẵn sàng
                                     *    3 - có trong phòng và đang chơi game
                                     *    4 - vị trí tự động chơi game vì người chơi đã thoát đột ngột
                                     *    5 - sử dụng AI cho vị trí này 
                                     */
        private int RoomStatus;     /*
                                     *    Phòng đang đợi (WAITING)
                                     *    Phòng đang chơi (PLAYING)
                                     */
        public const int NOT_IN_ROOM = 0;
        public const int NOT_READY = 1;
        public const int READY = 2;
        public const int PLAYING = 3;
        public const int AFK = 4;
        public const int AI = 5;
        public const int ROOM_WAITING = 0;
        public const int ROOM_PLAYING = 1;
        protected Room(Lobby lobby){
            if (lobby == null)
                throw new Exception("Lobby can not be a null instances");

            this.host = -1;
            this.lastwinner = -1;
            this.BetMoney = 1;
            this.game = null;
            this.PlayerStatus = new int[]{0, 0, 0, 0}; // NOT_IN_ROOM
            this.clients = new Client[]{null, null, null, null};
            this.lobby = lobby;
            this.RoomStatus = Room.ROOM_WAITING;
        }
        public static Room Create(Lobby lobby){
            Room room = null;
            
            try{
                room = new Room(lobby);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }
            
            return room;
        }
        public int Add(Client player){
            if (player ==  null)
                throw new Exception("Player can not be a null instance");

            if (this.clients.Where(player) != -1)
                throw new Exception("User has been in this room");

            lock(this.clients){
                int index = this.PlayerStatus.Where(Room.NOT_IN_ROOM);
                if (index == -1)
                    throw new Exception("Room is full");

                this.clients[index] = player;
                this.PlayerStatus[index] = Room.NOT_READY;
                
                this.host = this.host == -1 ? index : this.host;

                return index;
            }
        }
        public int Remove(Client player){
            if (player == null)
                throw new Exception("Player can not be a null instance");

            int index = this.clients.Where(player);
            if (index == -1)
                throw new Exception("Player do not exist in room");

            lock(this.clients){
                this.clients[index] = null;
                if (this.PlayerStatus[index] == Room.PLAYING)
                    this.PlayerStatus[index] = Room.AFK;
                else
                    this.PlayerStatus[index] = Room.NOT_IN_ROOM;

                this.host = this.host == index ? -1 : this.host;
                this.lastwinner = this.lastwinner == index ? -1 : this.lastwinner;

                if (this.host == -1)
                    for (int add = 0; add < this.clients.Count(); add++){
                        int i = (index + add) % this.clients.Count();
                        if (this.PlayerStatus[i] != Room.NOT_IN_ROOM &&
                            this.PlayerStatus[i] != Room.AFK &&
                            this.PlayerStatus[i] != Room.AI){
                            this.host = i;
                            break;
                        }
                    }
            }
            
            return index;
        }
        public void Ready(Client player){
            if (player == null)
                throw new Exception("Player cannot a null instance");

            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            int index = this.clients.Where(player);
            
            if (index == this.host)
                throw new Exception("You are the host. It is unnecessary to ready");

            if (index < 0 || index >= this.clients.Count() || this.clients[index] == null)
                throw new Exception("Player[{0}] do not exist in room");
            
            if (this.PlayerStatus[index] != Room.NOT_READY)
                throw new Exception("Player cannot ready now");

            this.PlayerStatus[index] = Room.READY;
        }
        public void UnReady(Client player){
            if (player == null)
                throw new Exception("Player cannot a null instance");

            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            int index = this.clients.Where(player);
            
            if (index < 0 || index >= this.clients.Count() || this.clients[index] == null)
                throw new Exception("Player[{0}] do not exist in room");

            if (this.PlayerStatus[index] != Room.READY)
                throw new Exception("Player cannot unready now");

            this.PlayerStatus[index] = Room.NOT_READY;
        }
        public void SetAI(int index){
            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            if (this.PlayerStatus[index] == Room.AI)
                throw new Exception("This position has already been a AI");
            
            if (this.PlayerStatus[index] != Room.NOT_IN_ROOM)
                throw new Exception("This position was hold by another player");

            this.PlayerStatus[index] = Room.AI;
        }

        public void RemoveAI(int index){
            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing. Please wait for ending");

            if (this.PlayerStatus[index] == Room.NOT_IN_ROOM)
                throw new Exception("This status is a empty slot");

            if (this.PlayerStatus[index] != Room.AI)
                throw new Exception("This position is not a AI slot");
            
            this.PlayerStatus[index] = Room.NOT_IN_ROOM;
        }
        public void SetBetMoney(Client client, int betmoney){
            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Everybody are playing, dont set bet money");

            int index = this.clients.Where(client);

            if (index != this.host)
                throw new Exception("Only host can adjust betmoney");

            if (betmoney < 0)
                throw new Exception("Bet money must be positive number");
            
            if (betmoney > 1e6)
                throw new Exception("Bet money is too big");

            this.BetMoney = betmoney;
        }
        public Game StartGame(){
            if (this.RoomStatus == Room.ROOM_PLAYING)
                throw new Exception("Room are playing");
                
            if (this.PlayerStatus.CountDiff(Room.NOT_IN_ROOM) < 2)
                throw new Exception("It need at least 2 player to start game");

            lock(this.clients){
                int ready = this.PlayerStatus.Count(element:Room.READY) + 1; // Chủ phòng không cần ready
                int ai = this.PlayerStatus.Count(element:Room.AI);

                int inroom = this.PlayerStatus.CountDiff(element:Room.NOT_IN_ROOM);

                if (ready + ai < inroom)
                    throw new Exception("Someone hasn't been ready yet");

                //Continue at this
                try{
                    this.game = Game.Create(this.clients, this.PlayerStatus, this.lastwinner);
                }
                catch(Exception e){
                    this.WriteLine(e.Message);
                    return null;
                }
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null)
                        this.PlayerStatus[i] = Room.PLAYING;
            }
            
            this.RoomStatus = Room.ROOM_PLAYING;
            return this.game;
        }

        public void Refresh(){
            for (int i = 0; i < this.clients.Count(); i++){
                if (this.clients[i] == null)
                    this.PlayerStatus[i] = Room.NOT_IN_ROOM;
            
                if (this.lastwinner == i && this.PlayerStatus[i] == Room.NOT_IN_ROOM)
                    this.lastwinner = -1;
            }
        }

        public void StopGame(int winner){
            if (this.RoomStatus == Room.ROOM_WAITING)
                throw new Exception("Room are waiting, no game to stop");

            lock(this.clients){
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null)
                        this.PlayerStatus[i] = Room.NOT_READY;
                
                this.game = null;
            }
            
            this.lastwinner = winner;
            this.RoomStatus = Room.ROOM_WAITING;
            this.Refresh();
        }
        public void Destroy(){
            foreach(var client in this.clients)
                if (client != null){
                    this.Remove(client);
                    this.lobby.Add(client);
                }
        }
        public string GeneralInfo(){
            return "{0},{1},{2}".Format(this.clients.CountRealInstance(), this.BetMoney, this.RoomStatus);
        } 
        public string ToString(int index){
            if (index < 0 || index > this.clients.Count() || this.clients[index] == null)
                throw new Exception("User[{0}] is not exist in room".Format(index));

            // Lấy thông tin của bàn chơi dưới cái nhìn của người chơi thứ index
            string[] arr = new string[this.clients.Count() + 2];

            // Thông tin của người chơi thứ index
            arr[0] = this.BetMoney.ToString();

            int count = 2;
            for (int add = 0; add < this.clients.Count(); add++){
                int i = (index + add) % this.clients.Count();
                if (this.clients[i] == null)
                    arr[count] = "none,0,{0}".Format(this.PlayerStatus[i]);
                else
                    arr[count] = "{0},{1}".Format(this.clients[i].UserInfo(), this.PlayerStatus[i]);

                if (i == this.host){
                    arr[1] = (count - 2).ToString();
                }
                count += 1;
            }

            string ret = String.Join(",", arr);
            return ret;
        }

        public int GetStatus(int index){
            return this.PlayerStatus[index];
        }
    }
}