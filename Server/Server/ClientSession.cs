using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    sealed public class ClientSession : Session{
        /* 
        # Mục đích : Đảm nhận luồng xử lý khi một client kết nối với server
        # Các thuộc tính : ClientSession chứa socket đang giữ kết nối với client,
        #                  .. đồng thời giữ các thông tin của một client như user, 
        #                  .. room, lobby, ...
        # Hoạt động đặc biệt : Điểm đặc biệt của ClientSession là
        #                    .. ngoài nhận thông điệp từ các session khác, nó còn nhận
        #                    .. thông điệp từ máy client đang điều khiển nó.
        */
        private SimpleSocket client;
        private User user;
        private Thread ClientThread;
        private bool ClientStop;
        public override string Name => "Client";
        private LobbySession lobby;
        private RoomSession room;
        private GameSession game;
        private OutdoorSession outdoor;
        public ClientSession(SimpleSocket socket, OutdoorSession outdoor) : base(){
            this.client = socket;
            this.ClientThread = null;
            this.user = null;
            this.ClientStop = false;
            this.outdoor = outdoor;
            this.lobby = null;
            this.room = null;
            this.game = null;
        }
        private void ReceiveFromClient(){
            string last_message = null;

            while(this.ClientStop == false){
                string all_message = this.client.Receive();
                
                if (all_message == null){
                    this.Send(this, "Disconnect");
                    break;
                }

                if (last_message != null)
                    all_message = last_message + all_message;

                // Nếu message cuối cùng vẫn chưa hoàn tất, đặt flag = 1
                int flag = all_message.Last() != '|' ? 1 : 0;
                
                string[] message = all_message.Split('|');

                if (flag == 1)
                    // Lưu giữ phần hiện tại của message cuối cùng
                    last_message = message.Last();
                else
                    last_message = null;

                // Gửi các thông điệp đã hoàn thiện đến bản thân
                for (int i = 0; i < message.Count() - 1; i ++){
                    // Vòng lặp dừng khi i >= message.Count() - 1 vì
                    // ..message cuối cùng sẽ rỗng nếu các thông điệp đều đủ
                    // ..hoặc message cuối cùng sẽ thiếu
                    // ..đây là một phần kết quả của hàm String.Split()
                    // ..Ví dụ : "ab|cd|".Split('|') = [ab] [cd] []
                    this.Send(this, message[i]);
                }
            }
        }
        public override void Solve(Object obj){
            Message message = (Message) obj;
            string name = message.name;
            
            bool bValid = false;
            if (this.id == message.id)
                bValid = true;
            
            if (this.lobby != null && this.lobby.id == message.id)
                bValid = true;

            if (this.room != null && this.room.id == message.id)
                bValid = true;

            if (this.game != null && this.game.id == message.id)
                bValid = true;

            if (this.outdoor != null && this.outdoor.id == message.id)
                bValid = true;

            if (bValid == false){
                this.client.Send("Warning:Cannot identify message!");
                return;
            }

            switch (name)
            {
                case "Login":
                    /*
                    # Nhận thông tin client đăng nhập
                    # Hành động : Khởi tạo thông tin User
                    #             .. Kiểm tra quyền user
                    #             Nếu đã có quyền Authenticated --> trả về thành công
                    #             .. + thông tin lobby
                    #             Nếu không có quyền thông báo thất bại  
                    */
                    if (this.user != null){
                        this.client.Send("Failure:You have already logged in!");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 2 || 
                    this.id != message.id){
                        this.client.Send("Warning:Cannot identify message!");
                        return;
                    }

                    this.Login(message.args[0], message.args[1]);
                    break;

                case "Signup":
                    /*
                    # Nhận thông tin đăng ký từ client
                    # Hành động : Cố gắng đăng ký, gửi thông báo
                    */
                    if (this.id != message.id || message.args == null || 
                    message.args.Count() != 2){
                        this.client.Send("Warning:Cannot identify message!");
                        return;
                    }

                    this.Singup(message.args[0], message.args[1]);
                    break;

                case "Logout":
                    if (message.args != null || this.id != message.id){
                        this.client.Send("Warning:Cannot identify message!");
                        return;
                    }
                    this.Logout();
                    if (this.lobby != null)
                        this.Send(this.lobby, "Logout");
                    if (this.room != null)
                        this.Send(this.room, "Logout");
                    if (this.game != null)
                        this.Send(this.game, "Logout");
                    break;

                case "LobbyInfo":
                    if (this.lobby == null || message.which != this.lobby.Name){
                        // Nếu message không đến từ lobby hoặc client không ở lobby
                        Console.WriteLine("{0}:Error in sender".Format(message));
                        return;
                    }

                    if (message.args == null){
                        Console.WriteLine("{0}:Error in parameters".Format(message));
                        return;
                    }

                    int n;
                    if (Int32.TryParse(message.args[0], out n) == false){
                        Console.WriteLine("{0}:Error in parameters".Format(message));
                        return;
                    }
                    
                    if (n * 2 != message.args.Count() - 1){
                        Console.WriteLine("{0}:Error in parameters".Format(message));
                        return;
                    }
                    
                    this.client.Send(message.MessageOnly());
                    break;
                case "Disconnect":
                    if (this.outdoor != null)
                        this.Send(this.outdoor, "Disconnect");
                    if (this.lobby != null)
                        this.Send(this.lobby, "Disconnect");
                    if (this.room != null)
                        this.Send(this.room, "Disconnect");
                    if (this.game != null)
                        this.Send(this.game, "Disconnect");
                    break;

                default:
                    this.client.Send("Warning:Cannot identify message!");
                    break;
            }
        }
        public override void Start(){
            base.Start();
            this.ClientStop = false;
            this.ClientThread = new Thread(this.ReceiveFromClient);
            this.ClientThread.Start();
       }
        public override void Stop(string mode = "normal"){
            this.ClientStop = true;

            Thread.Sleep(100);
            while(this.ClientThread.IsAlive) {
                Console.WriteLine("Wait for threads Client {0}".Format(this.id));
                Thread.Sleep(1000);
            }
            this.ClientThread = null;

            base.Stop(mode);
        }
        public override void Destroy(string mode = "normal"){
            this.Logout();
            
            this.client.Close();
            this.client = null;

            base.Destroy(mode);
        }
        public bool IsAuthenticated(){
            if (this.user == null)
                return false;

            if (this.user.HavePermission(UserPermission.AUTHENTICATED))
                return true;
            return false;
        }
        private object Login(string username, string pass){        
            User tmp = new User();

            try{
                tmp.Authenticate(username, pass);
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                string m = "Failure:" + e.Message;
                this.client.Send(m);
                return null;
            }

            this.client.Send("Successfully:Login");
            this.Send(this.outdoor, "JoinLobby");
            this.user = tmp;
            return null;
        }
        private object Singup(string username, string pass){
            User admin = User.__administrator__;
            try{
                UserCollection.__default__.AddToDatabase(
                    admin,              // representation 
                    username,       
                    pass,               
                    1);                 // init money
            }
            catch(Exception e){
                string M = "Failure:" + e.Message;
                this.client.Send(M);
                return null;
            }

            // Gửi về thông báo đã đăng ký thành công với tên tài khoản đã đăng ký
            this.client.Send("Successfully:Signup,{0}".Format(username));
            return true;
        }
        private object Logout(){
            if (this.user == null){
                this.client.Send("Failure:You have not logged in yet");
                return null;
            }

            this.user.Destroy();
            this.client.Send("Successfully:Logout");
            this.user = null;
            return true;
        }
        public bool Join(LobbySession lobby){
            bool bSuccess = lobby.Add(this);
            
            if (bSuccess){
                this.lobby = lobby;
                this.room = null;
                this.game = null;
                this.outdoor = null;
            }
            return bSuccess;
        }
        public bool Join(RoomSession room){
            bool bSuccess = room.Add(this);
            
            if (bSuccess){
                this.room = room;
                this.lobby = null;
                this.game = null;
                this.outdoor = null;
            }
            return bSuccess;
        }

        public bool Join(OutdoorSession outdoor){
            bool bSuccess = outdoor.Add(this);
            
            if (bSuccess){
                this.room = null;
                this.lobby = null;
                this.game = null;
                this.outdoor = outdoor;
            }
            return bSuccess;
        }

        public bool IsAlive(){
            return this.client != null;
        }

    }
}