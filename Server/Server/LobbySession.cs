using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class LobbySession : Session{
        /*
         * Mục đích : Đại diện Lobby dưới dạng một thực thể phản ứng nhanh.
         * Thuộc tính : 
         *      + lobby             : Lưu giữ lobby mà nó đại diện.
         *      + outdoorsession    : Lưu giữ session đại diện cho outdoor trong lobby.
         *      + clientsessions    : Lưu giữ các session đại diện cho các client trong lobby.
         *      + roomsessions      : Lưu giữ các session đại diện cho các room trong lobby.
         * Khởi tạo:
         *      + LobbySession(Lobby, OutdoorSession) : Không thể truy cập trực tiếp.
         *      + Create(Lobby, OutdoorSession)       : Khởi tạo với lobby và outdoorsession chỉ định.
         * Phương thức :
         *      + Solve(Message)        : Xem chi tiết tại lớp Session.
         *      + Destroy()             : Xem chi tiết tại lớp Session.
         *      + Add(ClientSession)    : Thêm một client vào lobby.
         *      + Remove(ClientSession) : Loại bỏ một client ra khỏi lobby.
         *      + ToString()            : Trả về thông tin của lobby.
         *      + UpdateForClients()    : Gửi thông tin của các phòng trong lobby 
         *                                .. đến tất cả các client trong lobby.
         */
        public override string Name => "LobbySession";
        private Lobby lobby;
        private OutdoorSession outdoorsession;
        private ClientSession[] clientsessions;
        private RoomSession[] roomsessions;
        private Thread LobbyThread;
        private bool LobbyStop;
        private string oldstatus;
        protected LobbySession(Lobby lobby, OutdoorSession outdoor) : base(){
            if (lobby == null || outdoor == null)
                throw new Exception("Lobby and outdoor cant be the null instances");

            this.lobby = lobby;
            this.outdoorsession = outdoor;

            this.clientsessions = new ClientSession[Lobby.MAX_CLIENT];

            this.roomsessions = new RoomSession[Lobby.MAX_ROOM];
            for (int i = 0; i < Lobby.MAX_ROOM; i++){
                this.roomsessions[i] = new RoomSession(this.lobby.rooms[i], this);
                this.roomsessions[i].Start();
            }
        }
        public static LobbySession Create(Lobby lobby, OutdoorSession outdoor){
            LobbySession lobbysession = null;
            
            try{
                lobbysession = new LobbySession(lobby, outdoor);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }

            return lobbysession;
        }
        private void WaitForUpdate(){
            string status;
            this.LobbyStop = false;
            while(this.LobbyStop == false){
                Thread.Sleep(300);
                status = RoomCollection.__default__.GetLobbyInfo(this.lobby.id);
                if (status == this.oldstatus)
                    continue;

                this.Send(this, "UpdateLobby");
            }
        }
        private void StartWaitForUpdate(){
            this.LobbyThread = new Thread(this.WaitForUpdate);
            this.LobbyThread.Start();
        }
        private void StopWaitForUpdate(){
            this.LobbyStop = true;
            while(this.LobbyThread.IsAlive == true)
                Thread.Sleep(300);
        }
        public override void Solve(Object obj){
            /* 
             * Loại bỏ các trường hợp ngoại lệ
             */

            Message message = (Message) obj;

            bool bFailure = this.clientsessions.FindById(message.id) == -1;
            bFailure &= this.roomsessions.FindById(message.id) == -1;
            bFailure &= this.outdoorsession.id != message.id;
            bFailure &= this.id != message.id;

            if (bFailure){
                this.WriteLine("Message is not compatible with any session");
                return;
            }
            
            switch(message.name){
                case "JoinRoom":{
                    // Nhận thông tin vào phòng của một client
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message must be come from client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message need an argument (room number)");
                        return;
                    }

                    int RoomIndex;
                    if (Int32.TryParse(message.args[0], out RoomIndex) == false){
                        this.WriteLine("Parameters is incorrect");
                        return;
                    }
                    ClientSession user = null;
                    RoomSession room = null;
                    try{    
                        user = (ClientSession) this.clientsessions.GetById(message.id);
                        room = this.roomsessions[RoomIndex];
                        room.Add(user);
                        user.Join(room);
                        this.Remove(user);
                    }
                    catch(Exception e){
                        this.WriteLine(e.Message);
                        this.Send(user, "Failure:JoinRoom,{0}".Format(e.Message));
                        return;
                    }
  
                    //LobbyCollection.__default__.Change(this.lobby.id, this.ToString());
                    this.UpdateForClients();
                    break;
                }
                case "Logout":{
                    // Nhận thông tin đăng xuất của client
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message must be come from Client");
                        return;
                    }
                    
                    if (message.args != null){
                        this.WriteLine("Message must be no parameter");
                        return;
                    }

                    try{
                        ClientSession user = (ClientSession)this.clientsessions.GetById(message.id);
                        this.Remove(user);
                        user.Logout();
                        this.outdoorsession.Add(user);
                        user.Join(this.outdoorsession);
                    }
                    catch(Exception e){
                        this.WriteLine(e.Message);
                        return;
                    }
                    break;
                }
                case "Disconnect":{
                    // Nhận thông tin mất kết nối của client
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message do not come from client (socket)");
                        return;
                    }
                    
                    if (message.args != null){
                        this.WriteLine("Message hasn't to contain parameters");
                        return;
                    }

                    ClientSession client = (ClientSession)this.clientsessions.GetById(message.id);
                    this.Remove(client);
                    client.Join(this.outdoorsession);
                    this.outdoorsession.Add(client);
                    break;
                }
                case "UpdateLobby":{
                    if (this.roomsessions.FindById(message.id) == -1 && message.id != this.id){
                        this.WriteLine("Message must come from Room or Lobby");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message hasn't to contain any parameters");
                        return;
                    }
                    //if (message.id != this.id)
                    //    LobbyCollection.__default__.Change(this.lobby.id, this.ToString());
                    this.UpdateForClients();
                    break;
                }
                case "Payin":{
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message must be come from client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message need a parameter");
                        return;
                    }

                    ClientSession clientsession = (ClientSession)this.clientsessions.GetById(message.id);
                    
                    int money = 0;
                    switch (message.args[0]){
                        case "ANTN2017" : 
                            money = 10000;
                            break;
                        case "LTMCB2017":
                            money = 20000;
                            break;
                        case "UIT2017":
                            money = 50000;
                            break;
                    }

                    if (money == 0){
                        this.Send(clientsession, "Failure:Payin,Code is incorrect");
                        return;
                    }
                    
                    clientsession.client.user.ChangeMoney(money);
                    clientsession.client.user.GetInfo();
                    this.Send(clientsession, "Success:Payin,{0}".Format(clientsession.client.user.money));
                    break;
                }
                case "LobbyInfo":{
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message must come from Client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message hasn't to contain any parameters");
                        return;
                    }

                    int index = this.clientsessions.FindById(message.id);
                    
                    this.Send(this.clientsessions[index], 
                        "LobbyInfo:{0}"
                        .Format(RoomCollection.__default__.GetLobbyInfo(this.lobby.id)));
                    break;
                }
                default:{
                    this.WriteLine("Cannot identify message");
                    break;
                }
            }
        }
        public override void Start(){
            this.StartWaitForUpdate();
            base.Start();
        }
        public override void Stop(string mode = "normal"){
            this.StopWaitForUpdate();
            base.Stop();
        }
        public void UpdateForClients(){
            this.oldstatus = RoomCollection.__default__.GetLobbyInfo(this.lobby.id);
            foreach(ClientSession client in this.clientsessions)
                if (client != null)
                    this.Send(client, "LobbyInfo:{0}".Format(oldstatus));
        }
        public void Add(ClientSession usersession){
            if (usersession.client.IsAlive() == false 
            || usersession.client.IsLogin() == false){
                this.outdoorsession.Add(usersession);
                usersession.Join(this.outdoorsession);
                return;
            }

            int index = this.lobby.Add(usersession.client.user.username);
            this.clientsessions[index] = usersession;
            this.Send(usersession, "LobbyInfo:{0}".Format(this));
        }
        public void Remove(ClientSession usersession){
            int index = this.lobby.Remove(usersession.client.user.username);
            this.clientsessions[index] = null;
        }
        public override string ToString(){
            return this.lobby.ToString();
        }
        public override void Destroy(string mode = "normal"){
            foreach(var room in this.roomsessions)
                if (room != null)
                    room.Destroy(mode);

            foreach(var client in this.clientsessions)
                if (client != null){
                    this.Remove(client);
                    client.Join(this.outdoorsession);
                    this.outdoorsession.Add(client);
                }
            base.Destroy(mode);
        }
    }
}