# GENERAL
## Mô hình server được mô phỏng theo dạng ngôi nhà
### Gate
>Nơi chấp nhận client, một client sau khi được chấp nhận sẽ được đẩy đến outdoor --- Tương ứng với bước connect ở client.
### Outdoor
> Nơi xử lý những client vô danh. --- Tương ứng giao diện StartGame ở client.
### Lobby
> Nơi xử lý những client đã đăng nhập nhưng chưa vào phòng --- Tương ứng giao diện sảnh đợi ở client.
### Room 
> Nơi xử lý những client đã vào phòng --- Tương ứng giao diện room ở client.
### Game
> Nằm trong room, chuyên xử lý game, client không cần quan tâm.