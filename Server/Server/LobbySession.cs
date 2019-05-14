using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class LobbySession : Session{
        private Lobby lobby;
        public LobbySession(OutdoorSession outdoor) : base(){
            this.lobby = Lobby.Create(this, outdoor);
        }
        public override string Name => "Lobby";
        public override void Solve(Object obj){
            /* 
            # Loại bỏ các trường hợp ngoại lệ
            */

            Message message = (Message) obj;

            int index_client = this.lobby.FindClientIndex(message.id);
            int index_room = this.lobby.FindRoomIndex(message.id);
            int id_outdoor = this.lobby.GetIdOutdoor();

            if (index_client == -1 && index_room == -1 && id_outdoor != message.id)
                return;
            
            int id;
            ClientSession client = null;
            RoomSession room = null;
            string name = message.name;
            switch(name){
                case "JoinRoom":
                    // Nhận thông tin vào phòng của một client
                    if (index_client == -1){
                        Console.WriteLine("From Lobby : Message must be come from client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        Console.WriteLine("From Lobby : Message need an argument (room number)");
                        return;
                    }

                    int index;
                    if (Int32.TryParse(message.args[0], out index) == false){
                        Console.WriteLine("From Lobby : Parameters is incorrect");
                        return;
                    }

                    client = this.lobby.GetClientById(message.id);
                    room = this.lobby.GetRoomByIndex(index);

                    try{
                        if(this.lobby.Join(client, room) == false){
                            this.Send(client, "Failure:Client or room is wrong");
                            return;
                        }
                    }
                    catch(Exception e){
                        Console.WriteLine(e);
                        this.Send(client, "Failure:{0}".Format(e.Message));
                        return;
                    }

                    //this.Send(client, "RoomInfo:...");    
                    this.lobby.UpdateForClients(this);
                    break;
                case "ClientLeave":
                    // Nhận thông tin client rời đi của room
                    if (index_room == -1)
                        return;

                    if (message.args == null || message.args.Count() != 1)
                        return;
                    
                    bool bSuccess = Int32.TryParse(message.args[0], out id);
                    
                    if (bSuccess == false){
                        Console.WriteLine("From lobby : Argument(s) must be a integer");
                        return;
                    }

                    client = this.lobby.GetClientById(id);
                    this.Pop(client);
                    this.Send(this.lobby.GetIdOutdoor(), "ClientLeave:{0}".Format(message.args[0]));

                    break;
                case "Logout":
                    // Nhận thông tin đăng xuất của client
                    if (index_client == -1)
                        return;
                    
                    if (message.args != null)
                        return;

                    client = this.lobby.GetClientById(message.id);
                    this.Pop(client);
                    break;
                case "Disconnect":
                    // Nhận thông tin mất kết nối của client
                    if (index_client == -1){
                        Console.WriteLine("From Lobby {0} : Message do not come from client".Format(this.id));
                        return;
                    }
                    
                    if (message.args != null){
                        Console.WriteLine("From Lobby {0} : Message hasn't to contain parameters".Format(this.id));
                        return;
                    }

                    client = this.lobby.GetClientById(message.id);
                    this.Pop(client);
                    this.Send(this.lobby.GetIdOutdoor(), "ClientLeave:{0}".Format(message.id));
                    break;
            
                case "UpdateRoom":
                    if (index_room == -1){
                        Console.WriteLine(
                            "From {0} {1} : Message do not come from Room".
                            Format(this.Name, this.id)
                        );
                        return;
                    }

                    if (message.args != null){
                        Console.WriteLine(
                            "From {0} {1} : Message hasn't to contain parameters"
                            .Format(this.Name, this.id)
                        );
                        return;
                    }

                    this.lobby.UpdateForClients(this);
                    break;
                default:
                    Console.WriteLine("From {0} {1} : Cannot identify message".Format(this.Name, this.id));
                    break;
            }
        }
        public bool Add(ClientSession client){
            bool bSuccess = this.lobby.Add(client);
            if (bSuccess){
                this.Send(client, "LobbyInfo:{0},{1}".Format(Lobby.MAX_ROOM,this));
            }
            return bSuccess;
        }
        public void Pop(ClientSession client){
            this.lobby.Pop(client);
        }

        public override string ToString(){
            return this.lobby.ToString();
        }
        public override void Destroy(string mode = "normal"){
            this.lobby.Destroy(this);
            base.Destroy(mode);
        }
    }
}