# ***LOBBY***
## Gửi 
* Đăng xuất &nbsp;: *Logout*
* Vào phòng : *JoinRoom:< index >*
* Nạp tiền &nbsp;&nbsp;&nbsp;&nbsp;: *Payin:< Code >*  
*Code*
    * "ANTN2017" &nbsp;&nbsp;- 10 000
    * "LTMCB2017" &nbsp;- 20 000
    * "UIT2017"&ensp;&ensp;&ensp;&ensp;- 50 000.
## NHẬN
* *LobbyInfo:< n - số phòng >,< số người phòng 0 >,< tiền cược phòng 0 >,...,< số người phòng n-1 >,< tiền cược phòng n - 1 >*
    > Hiển thị thông tin các phòng trong lobby.
* *Success:Logout*
    > Chuyển user ra khỏi lobby trở về outdoor.
* *Success:JoinRoom*
    > Chuyển user ra khỏi lobby và vào phòng.
* *Success:Payin,< newmoney >*
    > Hiển thị và cập nhật lại số tiền của người dùng.
* *Failure:Logout,< reason >*
* *Failure:JoinRoom,< reason >*
* *Failure:Payin,< reason >*
	
