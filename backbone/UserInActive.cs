
static class UserInActive{
    public const int MAX_SERVED_USER = 10;    
    private User[] InActive = new User[MAX_SERVED_USER]; // Lưu ý : thiết lập chỉ get, không được set
                                                         //.. Có thể dùng list để thay thế (cần thay đổi cấu trúc các hàm phía sau) 
    public int top = 0; // Xác định vị trí cao nhất đã cấp trong mảng 
    public int count = 0; // Số lượng User đã cấp
    
    public int AddWait(User user){
        // thêm một user vào mảng, nếu mảng đầy thì chờ đến khi thêm vào được
        // trả về vị trí được thêm vào

        if (user == null || user.IsAuthenticated() == false)
            return -1;

        // Step 1 : Kiểm tra user đã tồn tại hay chưa
        for(int i = 0; i < MAX_SERVED_USER; i++)
        {
            if (InActive[i] != null && InActive[i].username = user.username)
                return i;
        }

        while (true)
        {
            if (InActive[top] == null)
            {
                InActive[top] = user;
                int tmp = top;
                top = (top + 1) % MAX_SERVED_USER;
                count++;
                return tmp;
            }
            top = (top + 1) % MAX_SERVED_USER;
        }
        return -1; // để đúng định dạng
    }


    public int Add(User user, int limit_try = MAX_SERVED_USER){
        // Thêm một user vào mảng, nếu mảng đầy thì thử với các vị trí khác đến khi tới giới hạn thì dừng4
        // trả về vị trí được thêm vào

        if (user == null || user.IsAuthenticated() == false)
            return -1;

        // Step 1 : Kiểm tra user đã tồn tại hay chưa
        for(int i = 0; i < MAX_SERVED_USER; i++)
        {
            if (InActive[i] != null && InActive[i].username = user.username)
                return i;
        }

        while (limit_try > 0)
        {
            if (InActive[top] == null)
            {
                InActive[top] = user;
                int tmp = top;
                top = (top + 1) % MAX_SERVED_USER;
                count++;
                return tmp;
            }
            top = (top + 1) % MAX_SERVED_USER;
            limit_try = limit_try - 1;
        }
        return -1;
    }

    public void Release(int index){
        // Loại bỏ một user ra khỏi hàng đợi
        InActive[index] = null;
        count = count - 1;
    }
}