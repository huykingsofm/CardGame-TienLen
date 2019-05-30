# ***ROOM***
(Tạo phòng mà không có lá bài nào được khởi tạo sẵn)
***
## Gửi
* Thoát khỏi room : *JoinLobby*
* Đặt tiền cược &nbsp;&nbsp;&nbsp;&nbsp; : *BetMoney:\<money>*
* Sẵn sàng &ensp;&ensp;&ensp;&ensp;&ensp;&ensp; : "Ready"
* Hủy sẵn sàng &ensp;&ensp; : "UnReady"
* Bắt đầu chơi &ensp; &ensp;&ensp;: "Start"
## Nhận
* *RoomInfo:\<tiền cược>,\<host-index>,\<name-0>,\<money-0>,\<status-0>,....,\<name-3>,\<money-3>,\<status-3>* 
	>* Hiển thị lại các thông tin trong phòng bao gồm tiền cược, chủ phòng và thông tin các người chơi.  
	>* Đối với các index bên trên, 0 là bản thân client, 1 là player kế bên trái, 2 player là đối diện và 3 là player bên phải.
	> * Trạng thái gồm 4 giá trị
	> 	* 0 - player không tồn tại trong phòng
	>	* 1 - chưa sẵn sàng
	>	* 2 - đã sẵn sàng
	>	* 3 - đang chơi game 
	
* *Success:Start*
	> Chuẩn bị nhận thêm một số message khác từ game. 
* *Success:Ready*
* *Success:UnReady*
* *Success:BetMoney*
* *Failure:Ready,\<reason>*
* *Failure:UnReady,\<reason>*
* *Failure:Start,\<reason>*
* *Failure:BetMoney,\<reason>*

	