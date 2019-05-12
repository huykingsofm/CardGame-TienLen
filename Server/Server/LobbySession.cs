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

            int index_client = this.lobby.FindClient(message.id);
            int index_room = this.lobby.FindRoom(message.id);
            int id_outdoor = this.lobby.GetIdOutdoor();

            if (index_client == -1 && index_room == -1 && id_outdoor != message.id)
                return;

            string name = message.name;
            switch(name){
                case "JoinRoom":
                    // Nhận thông tin vào phòng của một client
                    this.lobby.UpdateForClients(this);
                    break;
                case "ClientLeave":
                    // Nhận thông tin client rời đi của room
                    if (index_room == -1)
                        return;

                    if (message.args == null || message.args.Count() != 1)
                        return;
                    
                    int id;
                    bool bSuccess = Int32.TryParse(message.args[0], out id);
                    
                    if (bSuccess){
                        ClientSession client = this.lobby.GetClient(id);
                        this.Pop(client);
                    }
                    break;
                case "Logout":
                    // Nhận thông tin đăng xuất của client
                    if (index_client == -1)
                        return;
                    
                    if (message.args != null)
                        return;

                    {//block - to use local variables
                        ClientSession client = this.lobby.GetClient(message.id);
                        this.Pop(client);
                    }
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

                    {//block - to use local variables
                        ClientSession client = this.lobby.GetClient(message.id);
                        this.Pop(client);
                        this.Send(this.lobby.GetIdOutdoor(), "ClientLeave:{0}".Format(message.id));
                    }
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