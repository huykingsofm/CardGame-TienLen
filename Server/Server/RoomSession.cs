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

            if (this.clientsessions.FindById(message.id) == -1 && message.id != this.lobbysession.id){
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
                    this.UpdateForClients();
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
                    this.UpdateForClients();
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

                    this.gamesession = GameSession.Create(this.clientsessions, game, this.room.BetMoney);
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
                    this.UpdateForClients();
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
                default:{
                    this.WriteLine("Cannot identify message");
                    break;                
                }
            }
        }
        public void UpdateForClients(){
            for(int i = 0; i < this.clientsessions.Count(); i++)
                if (this.clientsessions[i] != null)
                    this.Send(this.clientsessions[i], "RoomInfo:{0}".Format(this.ToString(i)));
        }
        public void Add(ClientSession client){
            int index = this.room.Add(client.client);
            this.clientsessions[index] = client;
            this.Send(client, "Success:JoinRoom");
            this.UpdateForClients();
        }
        public void Remove(ClientSession client){
            int index = this.room.Remove(client.client);
            this.clientsessions[index] = null;
            this.UpdateForClients();
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