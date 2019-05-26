using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Json;
using System.IO;

namespace Server{
    public class Outdoor : Thing{
        /*
         * Mục đích : Đối tượng outdoor phục vụ các client khi chưa đăng nhập.
         * Thuộc tính : 
         *      + const MAX_CLIENT : số lượng client tối đa mà outdoor có thể phục vụ.
         *      + server           : một tcp giúp khởi tạo server.
         *      + clients[]        : một mảng chứa các client đang ở trong outdoor.
         *      + LastSlot         : vị trí (trong mảng) cuối cùng được cấp cho client.
         *      + lobby            : một lobby để có thể đẩy các client tới đó sau khi đã đăng nhập.
         *
         * Phương thức khởi tạo :
         *      + Outdoor()        : phương thức này không thể truy cập được
         *      + static Create()  : phương thức khởi tạo một outdoor, nếu không thể khởi tạo, 
         *                           .. trả về null.
         * Phương thức :
         *      + Add(Client, limit) : Thêm một client vào outdoor, với số lượng lượt kiểm tra giới hạn.
         *                             .. Nếu client đã đăng nhập, tự động đăng xuất. Xem thêm chi tiết 
         *                             .. bên dưới.
         *      + Remove(Client)     : Loại bỏ một client khỏi outdoor, xem chi tiết bên dưới.
         *      + Destroy(recursive) : Hủy bỏ outdoor bằng cách hủy tất cả các client trong nó.
         *                             .. Tùy chọn recursive cho phép hủy bỏ lobby trong nó trước 
         *                             .. khi tự hủy.
         */
        public override string Name => "Outdoor";
        public const int MAX_CLIENT = SimpleSocket.MAX_ACCEPTED_SOCKET;
        private TcpServer server;
        private Thread OutdoorThread;
        private bool OutdoorStop;
        public Client[] clients;
        private int LastSlot;
        public Lobby lobby;
        private Outdoor(){
            /*
             * Mục đích : Tạo ra một đối tượng outdoor phục vụ các client khi chưa đăng nhập
             *            Trong outdoor sẽ khởi thạo một lobby nhầm đẩy các client đã đăng nhập
             *            .. vào lobby để nó phục vụ.
             * Số lượng client được ở outdoor là giới hạn.
             */
            this.lobby = Lobby.Create(this);
            this.LastSlot = 0;
            this.clients = new Client[Outdoor.MAX_CLIENT];
        }
        public static Outdoor Create(){
            /* 
             * Mục đích : Tạo ra một outdoor, nếu không thể tạo, trả về null.
             */
            Outdoor outdoor;

            try{
                outdoor = new Outdoor();
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }

            return outdoor;
        }
        public int Add(Client client, int limit = Outdoor.MAX_CLIENT){
            /*
             * Mục đích : Thêm một client vào outdoor.
             * Hành động : 
             *      + Kiểm tra sự khả dụng của các thêm số.
             *      + Đảm bảo client không đăng nhập khi vào outdoor.
             *      + Khóa mảng clients[], tìm vị trí phù hợp với số  
             *        .. lượt tìm kiếm là giới hạn.
             *      + Nếu không thể tìm thấy vị trí phù hợp, tạo ra một
             *        .. exception. 
             *      + Nếu có thể tìm thấy vị trí, đặt client vào vị trí đó
             *        và trả về vị trí đã đặt vào.
             */
            if (client == null)
                throw new Exception("Client can't be a null instance");
            
            if (limit <= 0)
                throw new Exception("Limit time must be greater than 0");

            if (client.IsLogin() == true)
                client.Logout();

            lock(this.clients){
                int time = 0;
                while(this.clients[this.LastSlot] != null && time < limit){
                    this.LastSlot = (this.LastSlot + 1) % Outdoor.MAX_CLIENT;
                    time++;
                }

                if (this.clients[this.LastSlot] != null)
                throw new Exception("Outdoor is no more empty slot");

                this.clients[this.LastSlot] = client;
                this.WriteLine("Client enter to slot {0}", this.LastSlot);
                return this.LastSlot;
            }
        }
        public int Remove(Client client){
            /*
             * Mục đích : Loại bỏ client khỏi outdoor.
             * Hành động : 
             *      + Kiểm tra tham số truyền vào.
             *      + Tìm kiếm vị trí mà client hiện tại đang ở.
             *      + Đặt client đó là null.
             * Trả về vị trí mà client đã từng ở.
             */

            if (client == null)
                throw new Exception("Remove null instance is not allowed");

            int index = this.clients.Where(client);
            if (index == -1)
                throw new Exception("Client do not exist in outdoor");
            
            this.clients[index] = null;
            return index;
        }
        public void Destroy(bool recursive){
            /*
             * Mục đích : Hủy bỏ outdoor hiện tại. Sau khi hủy bỏ không thể thao tác trở lại.
             * Hành động : 
             *      + (tùy chọn) Hủy bỏ lobby đang kết nối với outdoor.
             *      + Hủy kết nối với các client trong outdoor.
             */

            if (recursive)
                this.lobby.Destroy(recursive);
                
            foreach(var client in this.clients)
                if (client != null)
                    client.Disconnect();

            this.lobby = null;
        }
    }
}