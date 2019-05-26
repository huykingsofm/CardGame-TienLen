using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Lobby : Thing{
        /*
         * Mục đích : Một lớp xử lý các thao tác với những client đã đăng nhập
         *            .. và đang ở sảnh chờ.
         * Thuộc tính :
         *      + clients : Lưu giữ thông tin các client đang ở trong nó.
         *      + rooms   : Lưu giữ thông tin các phòng đang kết nối với nó.
         *      + outdoor : Lưu trữ thông tin của outdoor đang kết nối với nó.
         *      + LastSlot: Chỉ số(trong clients[]) cấp cho client vào lobby 
         *                  .. cuối cùng.
         * Khởi tạo : 
         *      + Lobby(Outdoor)    : Không thể truy cập trực tiếp.
         *      + Create(Outdoor)   : Tạo ra một đối tượng Lobby với outdoor chỉ định.
         * Phương thức :
         *      + Add(Client, int)  : Thêm một client vào lobby.
         *      + Remove(Client)    : Loại bỏ một client trong lobby.
         *      + Destroy(bool)     : Hủy kết nối với các client trong lobby, có thể 
         *                            .. loại bỏ các phòng bên trong trước.
         *      + ToString()        : Nhận thông tin của tất cả các phòng trong lobby.
         */
        public override string Name => "Lobby";
        public const int MAX_ROOM = 10;
        public const int MAX_CLIENT = 100;
        private Client[] clients;   
        public Room[] rooms;
        private Outdoor outdoor;
        private int LastSlot;
        protected Lobby(Outdoor outdoor){
            /*
             * Mục đích : Tạo lobby với outdoor chỉ định.
             * Hành động : Kiểm tra tham số truyền vào và khởi
             *             .. tạo các thực thể cần thiết.
             */
            if (outdoor == null)
                throw new Exception("Outdoor can be a null instance");

            this.clients = new Client[Lobby.MAX_CLIENT];
            this.rooms = new Room[Lobby.MAX_ROOM];
            this.outdoor = outdoor;
            this.LastSlot = 0;
            
            for (int i = 0; i < this.rooms.Count(); i++)
                this.rooms[i] = Room.Create(this);
        }
        public static Lobby Create(Outdoor outdoor){
            /*
             * Mục đích : Tạo ra một thực thể lobby với outdoor chỉ định.
             * Trả về null nếu không thể tạo.
             */
            Lobby lobby = null;
            try{
                lobby = new Lobby(outdoor);
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }
            return lobby;
        }        
        public int Add(Client client, int limit = Lobby.MAX_CLIENT){
            /*
             * Mục đích : Thêm một client vào lobby
             * Hành động :
             *      + Kiểm tra các tham số.
             *      + Kiểm tra client đã đăng nhập hay chưa, nếu 
             *        .. chưa đăng nhập thì không thể thêm vào.
             *      + Kiểm tra client đã tồn tại trong phòng hay chưa.
             *      + Khóa mảng clients[], tìm kiếm vị trí phù hợp trong giới
             *        .. số lần cho phép.
             *      + Nếu tìm thấy vị trí phù hợp, thêm client vào.
             *      + Nếu không tìm thấy vị trí, tạo exception.
             */
            if (client == null)
                throw new Exception("client cant be a null instance");

            if (client.IsLogin() == false)
                throw new Exception("client must log in before enter lobby");

            if (this.clients.Contains(client))
                throw new Exception("client has been existed in server");

            int time = 0;
            lock(this.clients){
                while (time < limit && this.clients[this.LastSlot] != null){
                    this.LastSlot = (this.LastSlot + 1) % Lobby.MAX_CLIENT;
                    time++;
                }

                if (this.clients[this.LastSlot] != null)
                    throw new Exception("Lobby is no longer any empty slot");

                this.clients[this.LastSlot] = client;
                return this.LastSlot;
            }

        }
        public int Remove(Client client){
            /*
             * Mục đích : Loại bỏ một client ra khỏi lobby.
             * Hành động : 
             *      + Kiểm tra sự khả dụng của tham số.
             *      + Kiểm tra client có tồn tại trong lobby hay không.
             *      + Thực hiện khóa mảng clients[] loại bỏ client.
             */
            
            if (client == null)
                throw new Exception("client can not be a null instance");

            int index = this.clients.Where(client);
            if (index == -1)
                throw new Exception("client is not exist in lobby");

            lock(this.clients){
                this.clients[index] = null;
            }  

            return index;      
        }
        public void Destroy(bool recursive){
            /*
             * Mục đích : Hủy bỏ lobby.
             * Hành động :
             *      + (tùy chọn) Loại bỏ các phòng bên trong lobby.
             *      + Chuyển tất cả các client ra outdoor
             */
            if (recursive)
                foreach(Room room in this.rooms)
                    room.Destroy();

            foreach(Client client in this.clients.ToArray())
                if (client != null){
                    this.Remove(client);
                    this.outdoor.Add(client);
                }
        }
        public override string ToString(){
            /*
             * Mục đích : Trả về thông tin của các phòng trong lobby.
             * Thông tin có dạng : num_rooms,roominfo_1,roominfo_2,..,roominfo_n
             */
            string[] arr = new string[Lobby.MAX_ROOM + 1];

            arr[0] = Lobby.MAX_ROOM.ToString();
            for (int i = 0; i < Lobby.MAX_ROOM; i++)
                arr[i + 1] = this.rooms[i].GeneralInfo();

            return String.Join(",", arr);
        } 
    }
}