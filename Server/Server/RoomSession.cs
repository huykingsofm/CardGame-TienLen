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
        public RoomSession(Room room, LobbySession lobbysession) : base(){
            this.room = room;
            this.lobbysession = lobbysession;
            this.clientsessions = new ClientSession[4];
            this.gamesession = null;
        }
        public override void Solve(object obj){
            /* 
            # Loại bỏ các message không hợp lệ
            # Chỉ xử lý các client đang trong phòng
            */
            Message message  = (Message) obj;

            if (this.clientsessions.FindById(message.id) == -1 
            && message.id != this.lobbysession.id
            && (this.gamesession == null || message.id != this.gamesession.id) ){
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
                        this.room.Ready(clientsession.client);
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
                        this.room.UnReady(clientsession.client);
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
                    if (index != this.room.host){
                        this.Send(clientsession, "Failure:StartGame,You must be the host to start game");
                        return;
                    }
                    Game game = null;
                    try{
                        game = this.room.StartGame();
                    }
                    catch (Exception e){
                        this.WriteLine(e);
                        this.Send(clientsession, "Failure:Start,{0}".Format(e.Message));
                        return;
                    }

                    this.gamesession = GameSession.Create(
                        this, 
                        this.clientsessions, 
                        game, 
                        this.room.BetMoney
                    );
                    this.Send(clientsession, "Success:Start");

                    foreach(var client in this.clientsessions)
                        if (client != null)
                            client.Join(gamesession);
                    this.gamesession.Start();
                    
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

                    try{
                        this.room.SetBetMoney(clientsession.client, betmoney);
                    }
                    catch(Exception e){
                        this.Send(clientsession, "Failure:BetMoney,{0}".Format(e.Message));
                        return;
                    }
                    this.Send(clientsession, "Success:BetMoney");
                    this.Send(this.lobbysession, "UpdateRoom");
                    this.UpdateRoomForClients();
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
                    this.Send(this.lobbysession, "UpdateRoom");
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

                    this.lobbysession.Add(client);
                    client.Join(this.lobbysession);
                    this.Remove(client);
                    this.Send(this.lobbysession, "UpdateRoom");
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

                    if (this.room.GetStatus(index) == Room.PLAYING 
                    || this.room.GetStatus(index) == Room.AI){
                        this.WriteLine("Client is playing, dont leave");
                        return;
                    }

                    this.lobbysession.Add(client);
                    client.Join(this.lobbysession);
                    this.Remove(client);
                    this.Send(this.lobbysession, "UpdateRoom");
                    break;
                }
                case "UpdateGame":{
                    if (this.gamesession == null || message.id != this.gamesession.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }
                    this.UpdateGameForClients();
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
                    this.Send(this.lobbysession, "UpdateRoom");
                    break;
                }
                case "PlayingCard":{
                    if (this.gamesession == null){
                        this.WriteLine("Game has not still started yet");
                        return;
                    }

                    if (this.gamesession.id != message.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args == null){
                        this.WriteLine("Message need some parameters but not found any");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false){
                        this.WriteLine("Error in parameters");
                        return;
                    }

                    string playingcard = String.Join(',', message.args.Take(1, -1));
                    this.UpdatePlayingCardForClients(index, playingcard);
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

                    for (int i = 0; i < this.clientsessions.Count(); i++)
                        if (this.clientsessions[i] != null){
                            int dummywinner = (4 + winner - i) % 4;
                            this.Send(this.clientsessions[i], "GameFinished:{0}".Format(dummywinner));
                            this.clientsessions[i].Join((GameSession) null);
                        }

                    this.room.StopGame(winner);
                    this.room.Refresh();
                    this.UpdateRoomForClients();
                    this.Send(this.lobbysession, "UpdateRoom");
                    this.gamesession.Destroy();
                    this.gamesession = null;
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

                    if (iplayer != this.room.host){
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
                    this.UpdateRoomForClients();
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
                    if (iplayer != this.room.host){
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
                    this.UpdateRoomForClients();
                    break;
                }
                default:{
                    this.WriteLine("Cannot identify message");
                    break;                
                }
            }
        }
        public void UpdateRoomForClients(){
            for(int i = 0; i < this.clientsessions.Count(); i++)
                if (this.clientsessions[i] != null)
                    this.Send(this.clientsessions[i], "RoomInfo:{0}".Format(this.ToString(i)));
        }
        public void UpdateGameForClients(){
            if (this.gamesession == null)
                throw new Exception("Game has not started yet");

             for(int i = 0; i < this.clientsessions.Count(); i++)
                if (this.clientsessions[i] != null){
                    this.Send(this.clientsessions[i], "GameInfo:{0}"
                        .Format(this.gamesession.GameInfo(i)));
                    this.Send(this.clientsessions[i], "OnTableInfo:{0}"
                        .Format(this.gamesession.OnTableInfo()));
                }
        }
        public void UpdateGameForClient(int index){
            if (this.gamesession == null)
                throw new Exception("Game has not started yet");

            if (index < 0 || index  >= this.clientsessions.Count() || this.clientsessions[index] == null)
                throw new Exception("Client[{0}] do not exist in room".Format(index));

            this.Send(this.clientsessions[index], "GameInfo:{0}"
                .Format(this.gamesession.GameInfo(index)));
            this.Send(this.clientsessions[index], "OnTableInfo:{0}"
                .Format(this.gamesession.OnTableInfo()));
        }
        public void UpdatePlayingCardForClients(int index, string PlayingCard){
            if (this.gamesession == null)
                throw new Exception("Game has not started yet");

            for(int i = 0; i < this.clientsessions.Count(); i++)
                if (this.clientsessions[i] != null){
                    int onturn = (4 + index - i) % 4;
                    this.Send(this.clientsessions[i], "PlayingCard:{0},{1}"
                        .Format(onturn, PlayingCard) );
                }
        }
        public void Add(ClientSession client){
            int index = this.room.Add(client.client);
            this.clientsessions[index] = client;
            this.Send(client, "Success:JoinRoom");
            this.UpdateRoomForClients();
            if (this.gamesession != null)
                this.UpdateGameForClient(index);
        }
        public void Remove(ClientSession client){
            int index = this.room.Remove(client.client);
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