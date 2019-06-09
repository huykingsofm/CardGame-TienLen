using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Client : Thing{
        /*
         * Mục đích : Một lớp xử lý các tác vụ liên quan đến client và user.
         * Thuộc tính : 
         *      + socket : Lưu giữ socket kết nối với client.
         *      + user   : Lưu giữ user của người dùng nếu họ đã đăng nhập.
         * Khởi tạo : 
         *      + Client()              : Không thể truy cập trực tiếp.
         *      + Create(SimpleSocket)  : Tạo ra một Client với socket chỉ định.
         * Phương thức :
         *      + Login(string username, string pass) : Hỗ trợ client đăng nhập.
         *      + Logout()                            : Hỗ trợ client đăng xuất.
         *      + Disconnect()                        : Hỗ trợ client hủy kết nối.
         *      + UserInfo()                          : Lấy thông tin của user gắn với client.
         *      + IsLogin()                           : Kiểm tra client đã đăng nhập chưa?  
         *      + Receive()                           : Xem Receive() của SimpleSocket.
         *      + Send(string)                        : Xem Send(string) của SimpleSocket.
         */
        public override string Name => "Client";
        public SimpleSocket socket;
        public User user;

        protected Client(SimpleSocket socket){
            if (socket == null)
                throw new Exception("Socket can be a null instance");
            this.socket = socket;
        }

        public static Client Create(SimpleSocket socket){
            Client client = null;
            try{
                client = new Client(socket);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }
            return client;
        }
        public void Login(string username, string password){
            try{
                this.user = new User(username, password);
            }
            catch (Exception e){
                this.user = null;
                throw e;
            }
        }

        public void Logout(){
            if (this.user == null)
                throw new Exception("Client hasn't logged in yet");
            
            this.user.Destroy();
            this.user = null;
        }

        public void Disconnect(){
            if (this.user != null)
                this.user.Destroy();
            this.socket.Close();
        }

        public string UserInfo(){
            if (this.user == null)
                throw new Exception("Log in before get info");

            return "{0},{1}".Format(this.user.username, this.user.money);
        }

        public bool IsLogin(){
            if (this.user == null || this.user.HavePermission(UserPermission.AUTHENTICATED) == false)
                return false;
            
            return true;
        }

        public bool IsAlive(){
            return this.socket.IsConnected();
        }

        public string Receive(){
            return this.socket.Receive();
        }

        public bool Send(string str){
            return this.socket.Send(str);
        }

        public string ComputeToken(){
            if (this.IsLogin() == false)
                throw new Exception("Client must loggin before compute token");

            string hashedpass = UserCollection.__default__.GetHashPassword(
                User.__administrator__,
                this.user.username
            );
            string IP = this.socket.GetIP();
            return Utils.HashMd5(hashedpass + IP);
        }
    }
}