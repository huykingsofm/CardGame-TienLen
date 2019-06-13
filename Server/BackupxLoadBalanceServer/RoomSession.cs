using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class RoomSession : Session{
        Room room;
        LobbySession lobbysession;
        ClientSession[] clientsessions;
        GameSession gamesession;
        public override string Name => "RoomSession";
        private Thread RoomUpdateThread;
        private bool RoomUpdateStop;
        private Thread WaitGameThread;
        private bool WaitGameStop;
        private string oldstatus;
        private bool oldgamestatus = false;
        public RoomSession(Room room, LobbySession lobbysession) : base(){
            this.room = room;
            this.lobbysession = lobbysession;
            this.clientsessions = new ClientSession[4];
            this.gamesession = null;
        }
        private void WaitForUpdate(){
            string status;
            this.RoomUpdateStop = false;
            while(this.RoomUpdateStop == false){
                Thread.Sleep(300);
                status = this.ToString(0);
                if (status == this.oldstatus)
                    continue;

                this.Send(this, "UpdateSelf");
            }
        }
        private void StartWaitForUpdate(){
            this.RoomUpdateThread = new Thread(this.WaitForUpdate);
            this.RoomUpdateThread.Start();
        }
        private void StopWaitForUpdate(){
            this.RoomUpdateStop = true;
            while(this.RoomUpdateThread.IsAlive == true)
                Thread.Sleep(300);
        }
        private void WaitForStartGame(){
            this.WaitGameStop = false;
            while(this.WaitGameStop == false){
                Thread.Sleep(300);
                bool gamestatus = this.room.GameExist();
                if (this.oldgamestatus == gamestatus)
                    continue;

                if (gamestatus == true)  
                    this.Send(this, "CreateGame");
                else
                    this.Send(this, "RemoveGame");
                this.oldgamestatus = gamestatus;
            }
        }
        private void StartWaitForStartGame(){
            this.WaitGameThread = new Thread(this.WaitForStartGame);
            this.WaitGameThread.Start();
        }
        private void StopWaitForStartGame(){
            this.WaitGameStop = true;
            while(this.WaitGameThread.IsAlive == true)
                Thread.Sleep(300);
        }
        public override void Solve(object obj){
            /* 
            # Loại bỏ các message không hợp lệ
            # Chỉ xử lý các client đang trong phòng
            */
            Message message  = (Message) obj;

            if (this.clientsessions.FindById(message.id) == -1 
            && (message.id != this.lobbysession.id)
            && (this.gamesession == null || message.id != this.gamesession.id) 
            && (message.id != this.id)){
                this.WriteLine("Can not identify message");
                return;
            }

            switch(message.name){
                case "Ready":{
                    /* 
                    # Nhận thông tin sẵn sàng từ người chơi (id của client) 
                    # Cố gắng sẵn sàng từ id nhận được, nếu thành công thì
                    # ..gửi thông báo đến các client, nếu thất bại bỏ qua.
                    */
                    ClientSession clientsession = (ClientSession) this.clientsessions.GetById(message.id);
                    if (clientsession == null){
                        this.WriteLine("This message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("This message dont need any parameters");
                        return;
                    }

                    try{
                        this.room.Ready(clientsession.client.user.username);
                    }
                    catch(Exception e){
                        this.Send(clientsession, "Failure:Ready,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(clientsession, "Success:Ready");
                    this.UpdateRoomForClients();
                    break;
                }
                case "UnReady":{
                    /* 
                    # Nhận thông tin bỏ sẵn sàng từ người chơi (id của client) 
                    # Cố gắng bỏ sẵn sàng từ id nhận được, nếu thành công thì
                    # ..gửi thông báo đến các client, nếu thất bại bỏ qua.
                    */
                    ClientSession clientsession = (ClientSession) this.clientsessions.GetById(message.id);
                    if (clientsession == null){
                        this.WriteLine("This message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("This message dont need any parameters");
                        return;
                    }

                    try{
                        this.room.UnReady(clientsession.client.user.username);
                    }
                    catch(Exception e){
                        this.Send(clientsession, "Failure:Ready,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(clientsession, "Success:UnReady");
                    this.UpdateRoomForClients();
                    break;
                }
                case "Start":{
                    /* 
                    # Nhận thông tin bắt đầu game từ người chơi (id của client) 
                    # Kiểm tra xem người chơi có phải là host không?
                    # Nếu là host thì gọi phương thức StartGame, nếu không bỏ qua.
                    */
                    ClientSession clientsession = (ClientSession) this.clientsessions.GetById(message.id);
                    int index = this.clientsessions.FindById(message.id);
                    if (index != this.room.GetHost()){
                        this.Send(clientsession, "Failure:Start,You must be the host to start game");
                        return;
                    }

                    if (this.room.GetRoomStatus() == Room.ROOM_PLAYING){
                        this.Send(clientsession, "Failure:Start,Game has ready started");
                        return;
                    }

                    try{
                        this.room.StartGame();
                    }
                    catch (Exception e){
                        this.Send(clientsession, "Failure:Start,{0}".Format(e.Message));
                        return;
                    }

                    this.Send(clientsession, "Success:Start");
                    RoomCollection.__default__.SetGame(this.room.id, true);
                    break;
                }
                case "CreateGame":{
                    Game game = this.room.CreateGame();
                    this.gamesession = GameSession.Create(
                        room    : this,
                        clients : this.clientsessions,
                        game    : game,
                        BetMoney: this.room.GetBetMoney()
                    );
                    foreach(var client in this.clientsessions)
                        if (client != null)
                            client.Join(gamesession);
                    this.gamesession.Start();
                    
                    break;
                }
                case "RemoveGame":{    
                    this.gamesession.Destroy();
                    this.gamesession = null;
                
                    int winner = this.room.GetLastWinner();
                    for (int i = 0; i < this.clientsessions.Count(); i++)
                        if (this.clientsessions[i] != null){
                            int dummywinner = (4 + winner - i) % 4;
                            this.Send(this.clientsessions[i], "GameFinished:{0}".Format(dummywinner));
                            this.clientsessions[i].Join((GameSession) null);
                        }
                    break;
                }
                case "BetMoney":{
                    ClientSession clientsession = (ClientSession) this.clientsessions.GetById(message.id);
                    if (clientsession == null){
                        this.WriteLine("This message must come from client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message need a parameter");
                        return;
                    }

                    int betmoney;
                    if (Int32.TryParse(message.args[0], out betmoney) == false){
                        this.WriteLine("Parameters of message must be an integer");
                        return;
                    }

                    int iplayer = this.clientsessions.FindById(message.id);
                    if (iplayer != this.room.GetHost()){
                        this.Send(this.clientsessions[iplayer], "Failure:Only host can set bet money");
                        return;
                    }

                    try{
                        this.room.SetBetMoney(betmoney);
                    }
                    catch(Exception e){
                        this.Send(clientsession, "Failure:BetMoney,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(clientsession, "Success:BetMoney");
                    this.Send(this.lobbysession, "UpdateLobby");
                    break;
                }
                case "Logout":{
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message need come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }

                    ClientSession client = (ClientSession)this.clientsessions.GetById(message.id);
                    if (client == null){
                        this.WriteLine("Client do not exist in room");
                        return;
                    }

                    client.Logout();
                    this.Remove(client);
                    client.Join(this.lobbysession);
                    this.lobbysession.Add(client);
                    this.Send(this.lobbysession, "UpdateLobby");
                    break;
                }
                case "Disconnect":{
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message need come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }

                    ClientSession client = (ClientSession)this.clientsessions.GetById(message.id);
                    if (client == null){
                        this.WriteLine("Client do not exist in room");
                        return;
                    }

                    this.Remove(client);
                    client.Join(this.lobbysession);
                    this.lobbysession.Add(client);
                    this.Send(this.lobbysession, "UpdateLobby");
                    break;
                }
                case "JoinLobby":{
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }

                    ClientSession client = (ClientSession)this.clientsessions.GetById(message.id);
                    int index = this.clientsessions.FindById(message.id);
                    if (client == null){
                        this.WriteLine("Client do not exist in room");
                        return;
                    }

                    if (this.room.GetPlayerStatus(index) == Room.PLAYING 
                    || this.room.GetPlayerStatus(index) == Room.AI){
                        this.WriteLine("Client is playing, dont leave");
                        return;
                    }

                    this.lobbysession.Add(client, notify:false);
                    client.Join(this.lobbysession);
                    this.Remove(client);
                    break;
                }
                case "UpdateRoom":{
                    if (this.gamesession == null || message.id != this.gamesession.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }
                    this.Send(this.lobbysession, "UpdateLobby");
                    break;
                }
                case "GameFinished":{
                    if (message.id != this.gamesession.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message need a parameters");
                        return;
                    }

                    int winner = 0;
                    if (Int32.TryParse(message.args[0], out winner) == false){
                        this.WriteLine("Parameters is incorrect");
                        return;
                    }
                    GameCollection.__default__.Remove(this.room.id);
                    this.room.StopGame(winner);
                    RoomCollection.__default__.SetGame(this.room.id, false);
                    break;
                }
                case "SetAI":{
                    int iplayer = this.clientsessions.FindById(message.id);
                    if (iplayer == -1){
                        this.WriteLine("This message must come from Client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message must have a parameters");
                        return;
                    }

                    if (iplayer != this.room.GetHost()){
                        this.Send(this.clientsessions[iplayer], "Failure:Only host can set AI");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false || index < 0 || index >= 4){
                        this.WriteLine("Parameter is incorrect");
                        return;
                    }
                    try{
                        this.room.SetAI((index + iplayer) % 4);
                    }
                    catch(Exception e){
                        this.Send(this.clientsessions[iplayer], "Failure:SetAI,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(this.clientsessions[iplayer], "Success:SetAI,{0}".Format(index));
                    break;
                }
                case "RemoveAI":{
                    int iplayer = this.clientsessions.FindById(message.id);
                    if (iplayer == -1){
                        this.WriteLine("This message must come from Client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message must have a parameters");
                        return;
                    }
                    if (iplayer != this.room.GetHost()){
                        this.Send(this.clientsessions[iplayer], "Failure:Only host can set AI");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false || index < 0 || index >= 4){
                        this.WriteLine("Parameter is incorrect");
                        return;
                    }
                    try{
                        this.room.RemoveAI((index + iplayer) % 4);
                    }
                    catch(Exception e){
                        this.Send(this.clientsessions[iplayer], "Failure:RemoveAI,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(this.clientsessions[iplayer], "Success:RemoveAI,{0}".Format(index));
                    break;
                }
                case "UpdateSelf":{
                    if (message.id != this.id){
                        this.WriteLine("This message must come from RoomSession");
                        return;
                    }
                    if (message.args != null){
                        this.WriteLine("This message dont need any parameters");
                        return;
                    }

                    this.UpdateRoomForClients();
                    break;
                }
                default:{
                    this.WriteLine("Cannot identify message");
                    break;                
                }
            }
        }
        public override void Start(){
            this.StartWaitForStartGame();
            this.StartWaitForUpdate();
            base.Start();
        }
        public override void Stop(string mode = "normal"){
            this.StopWaitForUpdate();
            this.StopWaitForStartGame();
            base.Stop(mode);
        }
        public void UpdateRoomForClients(){
            this.oldstatus = this.ToString(0);
            for(int i = 0; i < this.clientsessions.Count(); i++)
                if (this.clientsessions[i] != null)
                    this.Send(this.clientsessions[i], "RoomInfo:{0}".Format(this.ToString(i)));
        }
        public void UpdateGameForClient(int index){
            if (this.gamesession == null)
                throw new Exception("Game has not started yet");

            if (index < 0 || index  >= this.clientsessions.Count() || this.clientsessions[index] == null)
                throw new Exception("Client[{0}] do not exist in room".Format(index));

            this.Send(this.clientsessions[index], "GameInfo:{0}"
                .Format(this.gamesession.GetGameInfo(index)));
            this.Send(this.clientsessions[index], "OnTableInfo:{0}"
                .Format(this.gamesession.OnTableInfo()));
        }
        public void Add(ClientSession client){
            int index = this.room.Add(client.client.user.username);
            this.clientsessions[index] = client;
            this.Send(client, "Success:JoinRoom");
<<<<<<< HEAD
            this.UpdateRoomForClients();
            client.Join(this.gamesession);
=======

            if (this.gamesession != null)
                client.Join(this.gamesession);

            //this.UpdateRoomForClients();
>>>>>>> rebuild
            if (this.gamesession != null)
                this.UpdateGameForClient(index);
        }
        public void Remove(ClientSession client){
            int index = this.room.Remove(client.client.user.username);
            this.clientsessions[index] = null;
            this.UpdateRoomForClients();
        }
        public string ToString(int index){
            return this.room.ToString(index);
        }
        public override void Destroy(string mode = "normal"){
            if (this.gamesession != null){
                this.gamesession.Destroy();
            }

            foreach(var client in this.clientsessions)
                if (client != null){
                    this.Remove(client);
                    client.Join(lobbysession);
                    this.lobbysession.Add(client);
                }
            base.Destroy(mode);
        }
    }
}