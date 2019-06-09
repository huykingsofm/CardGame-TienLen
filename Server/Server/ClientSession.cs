using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class ClientSession : Session{
        /* 
         * Mục đích : Đại diện một Client dưới dạng một thực thể phản ứng nhanh.
         * Thuộc tính : 
         *      + client        : chứa client mà nó đại diện.
         *      + thread        : chứa thông tin của luồng nhận dữ liệu từ client.
         *      + stop          : hỗ trợ duy trì hoặc dùng luồng nhận dữ liệu từ client.
         *      + lobbysession  : chứa lobbysession nếu client đang ở lobby.
         *      + outdoorsession: chứa outdoorsession nếu client đang ở outdoor.
         *      + roomsession   : chứa roomsession nếu client đang ở room.
         *      + gamesession   : chứa gamesession nếu client đang chơi game.
         * 
         * Khởi tạo : 
         *      + ClientSession(Client, OutdoorSession) : Không được truy cập trực tiếp.
         *      + Create(Client, OutdoorSession)        : Khởi tạo một đối tượng đang ở outdooor.
         * Phương thức :
         *      + ReceiveFromClient()   : Tiếp nhận dữ liệu từ client.
         *      + Solve(Message)        : Xem chi tiết ở lớp Session.
         *      + Start()               : Xem chi tiết ở lớp Session.
         *      + Stop()                : Xem chi tiết ở lớp Session.
         *      + Destroy()             : Xem chi tiết ở lớp Session.
         *      + Login(string, string) : Hỗ trợ đăng nhập.
         *      + Logout()              : Hỗ trợ đăng xuất.
         *      + Signup(string, string): Hỗ trợ đăng ký.
         *      + Join(...)             : Thay đổi trạng thái vị trí của client.                 
         */

        // ######### THUỘC TÍNH #############
        public Client client;
        private Thread thread;
        private bool stop;
        public override string Name => "ClientSession";
        private LobbySession lobbysession;
        private RoomSession roomsession;
        private GameSession gamesession;
        private OutdoorSession outdoorsession;

        //############# KHỞI TẠO ##################
        protected ClientSession(Client client, OutdoorSession outdoorsession) : base(){
            if (client == null || outdoorsession == null)
                throw new Exception("Client and OutdoorSession can be null instances");
            
            this.client = client;
            this.thread = null;
            this.stop = false;

            this.outdoorsession = outdoorsession;
            this.lobbysession = null;
            this.roomsession = null;
            this.gamesession = null;
        }
        public static ClientSession Create(Client client, OutdoorSession outdoorsession){
            ClientSession clientsession = null;
            try{
                clientsession = new ClientSession(client, outdoorsession);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }
            return clientsession;
        }        
        
        //############ PHƯƠNG THỨC ################
        private void ReceiveFromClient(){
            string last_message = null;

            while(this.stop == false){
                string all_message = this.client.Receive();

                Thread.Sleep(50);
                
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
            
            bool bValid = false;
            if (this.id == message.id)
                bValid = true;
            
            if (this.lobbysession != null && this.lobbysession.id == message.id)
                bValid = true;

            if (this.roomsession != null && this.roomsession.id == message.id)
                bValid = true;

            if (this.gamesession != null && this.gamesession.id == message.id)
                bValid = true;

            if (this.outdoorsession != null && this.outdoorsession.id == message.id)
                bValid = true;

            if (bValid == false){
                this.WriteLine("Cannot identify message!");
                return;
            }

            switch (message.name)
            {
                case "Login":{
                    /*
                    # Nhận thông tin client đăng nhập
                    # Hành động : Khởi tạo thông tin User
                    #             .. Kiểm tra quyền user
                    #             Nếu đã có quyền Authenticated --> trả về thành công
                    #             .. + thông tin lobby
                    #             Nếu không có quyền thông báo thất bại  
                    */
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (this.client.IsLogin()){
                        this.client.Send("Failure:Login,You have already logged in!");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 2){
                        this.WriteLine("Message need two parameters");
                        return;
                    }

                    this.Login(message.args[0], message.args[1]);
                    break;
                }
                case "Signup":{
                    /*
                    # Nhận thông tin đăng ký từ client
                    # Hành động : Cố gắng đăng ký, gửi thông báo
                    */
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if( message.args == null || message.args.Count() != 2){
                        this.WriteLine("Message need two parameters");
                        return;
                    }

                    this.Singup(message.args[0], message.args[1]);
                    break;
                }
                case "Logout":{
                    if (message.id != this.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null || this.id != message.id){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }
                    
                    if (this.lobbysession != null)
                        this.Send(this.lobbysession, "Logout");
                    if (this.roomsession != null)
                        this.Send(this.roomsession, "Logout");
                    if (this.gamesession != null)
                        this.Send(this.gamesession, "Logout");
                    break;
                }
                case "LobbyInfo":{
                    if (this.lobbysession == null || message.which != this.lobbysession.Name){
                        // Nếu message không đến từ lobby hoặc client không ở lobby
                        this.WriteLine("Message must come from lobby");
                        return;
                    }

                    if (message.args == null){
                        this.WriteLine("Error in parameters");
                        return;
                    }

                    int n;
                    if (Int32.TryParse(message.args[0], out n) == false){
                        this.WriteLine("Error in parameters");
                        return;
                    }
                    
                    if (n * 3 != message.args.Count() - 1){
                        this.WriteLine("Error in parameters");
                        return;
                    }
                    
                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "RoomInfo":{
                    if (this.roomsession == null || message.id != this.roomsession.id){
                        this.WriteLine("Message must come from Room");
                        return;
                    }
                
                    if (message.args == null || message.args.Count() != 14){
                        this.WriteLine("Message must have 14 arguments but {0}", message.args.Count());
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "GameInfo":{
                    if ( (this.gamesession == null || message.id != this.gamesession.id) 
                    && (this.roomsession == null || message.id != this.roomsession.id ) ){
                        this.WriteLine("Message must come from Game or Room");
                        return;
                    }
                
                    if (message.args == null){
                        this.WriteLine("Message must have many arguments");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "Disconnect":{
                    if (message.id != this.id){
                        this.WriteLine("Disconnect must come from itself");
                        return;
                    }

                    if (this.outdoorsession != null)
                        this.Send(this.outdoorsession, "Disconnect");
                    if (this.lobbysession != null)
                        this.Send(this.lobbysession, "Disconnect");
                    if (this.roomsession != null)
                        this.Send(this.roomsession, "Disconnect");
                    if (this.gamesession != null)
                        this.Send(this.gamesession, "Disconnect");
                    break;
                }
                case "Success":{
                    if (this.id == message.id){
                        this.WriteLine("This message can not come from client");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "Failure":{
                    if (this.id == message.id){
                        this.WriteLine("This message can not come from client");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "JoinRoom":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client(socket)");
                        return;
                    }
                    if (this.lobbysession == null){
                        this.client.Send("Failure:JoinRoom,You must be in lobby to join room");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message must have an argument");
                        return;
                    }

                    this.Send(this.lobbysession, message.MessageOnly());
                    break;
                }
                case "JoinLobby":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message must not constain any argument");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Failure:JoinLobby,You must be in room before send this message");
                        return;
                    }

                    this.Send(this.roomsession, "JoinLobby");
                    break;
                }
                case "Payin":{
                    if (this.id != message.id){
                        this.WriteLine("Message must be come from Client");
                        return;
                    }

                    if (this.lobbysession == null){
                        this.client.Send("Failure:Payin,You must in lobby");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message need a parameter");
                        return;
                    }

                    this.Send(this.lobbysession, message.MessageOnly());
                    break;
                }
                case "Ready":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message must not constain any argument");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Failure:Ready,You must be in room before send this message");
                        return;
                    }

                    this.Send(this.roomsession, "Ready");
                    break;
                }
                case "UnReady":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message must not constain any argument");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Failure:UnReady,You must be in room before send this message");
                        return;
                    }

                    this.Send(this.roomsession, "UnReady");
                    break;
                }
                case "Start":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message must not constain any argument");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Failure:Start,You must be in room before send this message");
                        return;
                    }

                    this.Send(this.roomsession, "Start");
                    break;
                }                
                case "BetMoney":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message must constain an argument");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Failure:BetMoney,You must be in room before send this message");
                        return;
                    }

                    this.Send(this.roomsession, message.MessageOnly());
                    break;
                }
                case "Play":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from Client");
                        return;
                    }

                    if (message.args == null){
                        this.WriteLine("Message need some parameters");
                        return;
                    }

                    if (this.gamesession == null){
                        this.WriteLine("Client is not in any game");
                        return;
                    }

                    this.Send(gamesession, message.MessageOnly());
                    break;
                }
                case "Pass":{
                    if (message.id != this.id){
                        this.WriteLine("Message must come from Client");
                        return;
                    }

                    if (this.gamesession == null){
                        this.WriteLine("Client must be in game to send this message");
                        return;
                    }

                    if (message.args != null){
                        this.WriteLine("Message dont need any parameters");
                        return;
                    }

                    this.Send(this.gamesession, message.MessageOnly());
                    break;
                }
                case "PlayingCard":{
                    if ((this.roomsession == null || message.id != this.roomsession.id) &&
                        (this.gamesession == null || message.id != this.gamesession.id) ){
                        this.WriteLine("Message must be come from roomsession or room");
                        return;
                    }

                    if (message.args == null){
                        this.WriteLine("Message need some parameters but not found any");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "OnTableInfo":{
                    if ( (this.gamesession == null || message.id != this.gamesession.id) 
                    && (this.roomsession == null || message.id != this.roomsession.id ) ){
                        this.WriteLine("Message must come from Game or Room");
                        return;
                    }
                
                    if (message.args == null){
                        this.WriteLine("Message must have many arguments");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "UpdateMoney":{
                    if ( (this.gamesession == null || message.id != this.gamesession.id) 
                    && (this.roomsession == null || message.id != this.roomsession.id ) ){
                        this.WriteLine("Message must come from Game or Room");
                        return;
                    }
                
                    if (message.args == null || message.args.Count() != 4){
                        this.WriteLine("Message need 4 arguments");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "GameFinished":{
                    if (message.id != this.roomsession.id){
                        this.WriteLine("Message must come from Room");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message need a parameter");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "Time":{
                    if (this.gamesession == null || message.id != this.gamesession.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 2){
                        this.WriteLine("Message must have 2 parameters");
                        return;
                    }

                    this.client.Send(message.MessageOnly());
                    break;
                }
                case "SetAI":{
                    if (this.id != message.id){
                        this.WriteLine("This message must come from Client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message must have a parameters");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Client must be in room to send this message");
                        return;
                    }

                    this.Send(this.roomsession, message.MessageOnly());
                    break;
                }
                case "RemoveAI":{
                    if (this.id != message.id){
                        this.WriteLine("This message must come from Client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message must have a parameters");
                        return;
                    }

                    if (this.roomsession == null){
                        this.client.Send("Client must be in room to send this message");
                        return;
                    }

                    this.Send(this.roomsession, message.MessageOnly());
                    break;
                }
                case "Token":{
                    if (this.client.IsLogin()){
                        this.client.Send("Failure:Token,You have already logged in!");
                        return;
                    }

                    if (message.id != this.id){
                        this.WriteLine("This message must come from Client");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("This message need a parameter");
                        return;
                    }

                    string token = message.args[0];
                    try{
                        this.client.user = User.AuthenticateByToken(token, this.client);
                    }
                    catch(Exception e){
                        this.client.Send("Failure:Token,{0}".Format(e.Message));
                        return;
                    }

                    this.client.Send("Success:Login,{0},{1}"
                        .Format(this.client.user.username, this.client.user.money));
                    this.Send(this.outdoorsession, "JoinLobby");
                    break;
                }
                default:{
                    this.WriteLine("Cannot identify message!");
                    break;
                }
            }
        }
        public override void Start(){
            base.Start();
            this.stop = false;
            this.thread = new Thread(this.ReceiveFromClient);
            this.thread.Start();
       }
        public override void Stop(string mode = "normal"){
            if (this.thread == null)
                return;

            this.stop = true;

            Thread.Sleep(100);
            while(this.thread.IsAlive) {
                this.WriteLine("Wait for threads");
                Thread.Sleep(1000);
            }
            this.thread = null;

            base.Stop(mode);
        }
        public override void Destroy(string mode = "normal"){
            this.client.Disconnect();
            base.Destroy(mode);
        }
        private object Login(string username, string pass){        
            try{
                this.client.Login(username, pass);
            }
            catch (Exception e){
                this.WriteLine(e.Message);
                string m = "Failure:Login," + e.Message;
                this.client.Send(m);
                return null;
            }
            
            TokenCollection.__default__.Add(username, this.client.ComputeToken());

            this.client.Send("Success:Login,{0},{1}".Format(this.client.user.username, this.client.user.money));
            this.client.Send("Token:{0}".Format(this.client.ComputeToken()));
            this.Send(this.outdoorsession, "JoinLobby");
            return null;
        }
        private object Singup(string username, string pass){
            try{
                UserCollection.__default__.AddToDatabase(
                    User.__administrator__,              // representation 
                    username,       
                    pass,               
                    1);                                 // init money
            }
            catch(Exception e){
                string M = "Failure:Signup," + e.Message;
                this.client.Send(M);
                return null;
            }

            // Gửi về thông báo đã đăng ký thành công với tên tài khoản đã đăng ký
            this.client.Send("Success:Signup,{0}".Format(username));
            return true;
        }
        public object Logout(){
            try{
                this.client.Logout();
            }
            catch(Exception e){
                this.WriteLine(e.Message);
                return null;
            }
            this.client.Send("Success:Logout");
            return true;
        }
        public void Join(LobbySession lobby){
            this.lobbysession = lobby;
            this.roomsession = null;
            this.gamesession = null;
            this.outdoorsession = null;
        }
        public void Join(RoomSession room){
            this.roomsession = room;
            this.lobbysession = null;
            this.outdoorsession = null;
        }
        public void Join(OutdoorSession outdoor){
            this.roomsession = null;
            this.lobbysession = null;
            this.gamesession = null;
            this.outdoorsession = outdoor;
        }
        public void Join(GameSession gamesession){
            this.lobbysession = null;
            this.gamesession = gamesession;
            this.outdoorsession = null;
        }
        public User GetUser(){
            if (this.client.IsLogin() == false)
                return null;

            return this.client.user.Clone();
        }
    }
}