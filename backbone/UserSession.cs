// Tạo ra một class để quản lý khi client kết nối với server

class ClientSession : kế thừa từ MySocket/SocketModel{
    // ### ATTRIBUTES
    private int IndexInUserInActive; // Ta sẽ không lưu một User user, thay vào đó, ta sẽ có một mảng
                                     // .. User[] UserInActive, lưu các user đã được cấp quyền. Mục đích là để
                                     // .. tạo ra sự duy nhất khi cập nhật dữ liệu ở cơ sở dữ liệu nếu 
                                     // .. có nhiều client đăng nhập một user (mặc định là tắt chức năng này)
                                     // .. chức năng này lưu ở server.ini --> SingleUserOnMultiplePlace = false
    
    Room room;                       // Lưu phòng hiện tại mà user đang ở trong
                                     // nếu user không ở trong phòng(ở sảnh, hoặc chưa đăng nhập) --> room = null

    
    // ### CONSTRUCTOR
    // Ngoài constructor mặc định, ta định nghĩa thêm
    public ClientSession() : base(){
        IndexInUserInActive = -1;
        room = null;
    }

    public ClientSession(Socket socket) : base(socket){
        IndexInUserInActive = -1;
        room = null;
    }

    public ClientSession(User user, Socket socket) : base(socket){
        IndexInUserInActive = UserInActive.AddWait(user);
        room = null;
    }


    // ### METHOD
    private bool Connect(User user){
        // Thực hiện kết nối ClientSession hiện tại với một user(đã được cấp quyền)
        IndexInUserInActive = UserInActive.Add(user);
        return IndexInUserInActive != -1;
    }

    public bool Connect(Room room){
        // Kết nối ClientSession hiện tại với một room
        // Điều kiện cần là user đã được cấp quyền
        if (IndexInUserInActive == -1)
            return false;
        
        if (room == null)
            return false;

        if (room.Add(UserInActive[IndexInUserInActive]) != -1)
            this.room = room;
    }


    // BEHAVIOR METHOD
    public bool Login(string username, string pass){
        
    }
}