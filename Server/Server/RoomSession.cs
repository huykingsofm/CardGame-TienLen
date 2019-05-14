using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class RoomSession : Session{
        Room room;
        public override string Name => "Room";
        public int index{get; private set;}
        public RoomSession(LobbySession lobby, int index) : base(){
            this.room = new Room(lobby, index);
            this.index = index;
        }
        public override void Solve(object obj){
            /* 
            # Loại bỏ các message không hợp lệ
            # Chỉ xử lý các client đang trong phòng
            */
            Message message  = (Message) obj;
            int lobbyid = this.room.GetLobbyId();
            int index_client = this.room.FindIndexById(message.id);

            if (index_client == -1 && message.id != lobbyid){
                Console.WriteLine("From room : Can not identify message");
                return;
            }

            string name = message.name;
            switch(name){
                case "Ready":
                    /* 
                    # Nhận thông tin sẵn sàng từ người chơi (id của client) 
                    # Cố gắng sẵn sàng từ id nhận được, nếu thành công thì
                    # ..gửi thông báo đến các client, nếu thất bại bỏ qua.
                    */
                    break;
                case "UnReady":
                    /* 
                    # Nhận thông tin bỏ sẵn sàng từ người chơi (id của client) 
                    # Cố gắng bỏ sẵn sàng từ id nhận được, nếu thành công thì
                    # ..gửi thông báo đến các client, nếu thất bại bỏ qua.
                    */
                    break;
                case "StartGame":
                    /* 
                    # Nhận thông tin bắt đầu game từ người chơi (id của client) 
                    # Kiểm tra xem người chơi có phải là host không?
                    # Nếu là host thì gọi phương thức StartGame, nếu không bỏ qua.
                    */
                    break;
                case "Leave":
                    /* 
                    # Nhận thông tin rời phòng từ người chơi (id của client) 
                    # Cố gắng rời phòng từ id nhận được, nếu thành công thì
                    # ..gửi thông báo đến các client, nếu thất bại bỏ qua.
                    */
                    break;
                case "SetBetMoney":
                    break;
                case "Disconnect":
                    if (index_client == -1){
                        Console.WriteLine("From room : Message need come from client");
                        return;
                    }

                    if (message.args != null){
                        Console.WriteLine("From room : Message dont need any parameters");
                        return;
                    }

                    this.room.PopById(message.id);
                    this.Send(lobbyid, "ClientLeave:{0}".Format(message.id));
                    this.Send(lobbyid, "UpdateRoom");
                    break;
                default:
                    break;                
            }
        }
        public bool Add(ClientSession client){
            bool bSuccess = this.room.Add(client);
            return bSuccess;
        }
        public override string ToString(){
            return this.room.ToString();
        }
        public override void Destroy(string mode = "normal"){
            this.room.Destroy();
            base.Destroy(mode);
        }
    }
}