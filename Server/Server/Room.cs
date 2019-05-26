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
                                    #    0 - không trong phòng
                                    #    1 - có trong phòng nhưng chưa sẵn sàng
                                    #    2 - có trong phòng và đã sẵn sàng
                                    #    3 - có trong phòng và đang chơi game
                                    */
        private static int NOT_IN_ROOM = 0;
        private static int NOT_READY = 1;
        private static int READY = 2;
        private static int PLAYING = 3;
        protected Room(Lobby lobby){
            if (lobby == null)
                throw new Exception("Lobby can not be a null instances");

            this.host = -1;
            this.lastwinner = -1;
            this.BetMoney = 0;
            this.game = null;
            this.PlayerStatus = new int[]{0, 0, 0, 0}; // NOT_IN_ROOM
            this.clients = new Client[]{null, null, null, null};
            this.lobby = lobby;
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
                int index = this.clients.Where(null);
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
                this.PlayerStatus[index] = Room.NOT_IN_ROOM;

                this.host = this.host == index ? -1 : this.host;
                this.lastwinner = this.lastwinner == index ? -1 : this.lastwinner;

                if (this.host == -1)
                    for (int add = 0; add < this.clients.Count(); add++){
                        int i = (index + add) % this.clients.Count();
                        if (this.clients[i] != null){
                            this.host = i;
                            break;
                        }
                    }
                
            }
            
            return index;
        }
        public void Ready(Client player){
            int index = this.clients.Where(player);
            
            if (index < 0 || index >= this.clients.Count() || this.clients[index] == null)
                throw new Exception("Player[{0}] do not exist in room");
            
            this.PlayerStatus[index] = Room.READY;
        }
        public void SetBetMoney(Client client, int betmoney){
            int index = this.clients.Where(client);

            if (index != this.host)
                throw new Exception("Only host can adjust betmoney");

            if (betmoney < 0)
                throw new Exception("Bet money must be positive number");
            
            if (betmoney > 1e6)
                throw new Exception("Bet money is too big");

            this.BetMoney = betmoney;
        }
        public void UnReady(Client player){
            int index = this.clients.Where(player);
            
            if (index < 0 || index >= this.clients.Count() || this.clients[index] == null)
                throw new Exception("Player[{0}] do not exist in room");
            this.PlayerStatus[index] = Room.NOT_READY;
        }
        public Game StartGame(){
            if (this.clients.CountRealInstance() < 2)
                throw new Exception("It need at least 2 player to start game");

            lock(this.clients){
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null && this.PlayerStatus[i] != Room.READY)
                        throw new Exception("Someone hasn't been ready yet");
                
                this.game = Game.Create(this.clients, this.lastwinner);
                
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null)
                        this.PlayerStatus[i] = Room.PLAYING;
            }
            
            return this.game;
        }
        public void Destroy(){
            foreach(var client in this.clients)
                if (client != null){
                    this.Remove(client);
                    this.lobby.Add(client);
                }
        }
        public string GeneralInfo(){
            return "{0},{1}".Format(this.clients.CountRealInstance(), this.BetMoney);
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
                    arr[count] = "none,0,0";
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
    }
}