# ***GAME***
# Gửi
* Đánh bài : *Play:<lá bài 1>,<lá bài 2>,...*
    > *<lá bài i>* có dạng *<number_suit>* **[1]**. Ví dụ *2_3* là heo rô.
* Bỏ lượt &nbsp;&nbsp; : *Pass*
# Nhận
* *GameInfo:<thông tin bộ bài 0>,<số lá bài 1>,<số lá bài 2>,<số lá bài 3>,<chỉ số người đang tới lượt>,<cho phép bỏ lượt>*
    >   * Hiển thị game với các thông tin nhận được, bao gồm thông tin bộ bài của người chơi hiện tại, số lượng lá bài của những người chơi khác, chỉ số người đang tới lượt, khả năng bỏ lượt và số thời gian còn lại.
    >   * *<thông tin bộ bài>* là một chuỗi gồm *<số lượng lá bài>,<lá bài 1>,<lá bài 2>,...,<lá bài n>* **[2]**  
    Với *<lá bài i>* có dạng giống ***[1]***
    >   * *<cho phép bỏ lượt>* có 3 giá trị
    >       * -1 : người chơi không ở trong lượt, không cần quan tâm
    >       * 0 &nbsp; : người chơi không thể bỏ lượt 
    >       * 1 &nbsp; : người chơi có thể bỏ lượt.
* *Time:<chỉ số người đang tới lượt>,<thời gian còn lại>*
    > Hiển thị thời gian còn lại của người chơi đang đến lượt.
* *OnTableInfo:<số lượng bộ bài đang trên bàn>,<thông tin bộ bài 1>,<thông tin bộ bài 2>,...,<thông tin bộ bài n>*
    > * Hiển thị những bộ bài đã đánh ra trên bàn, chỉ có thông tin những bộ bài đang ngửa trên bàn, thông tin những bộ bài đã úp lại (đã qua lượt) sẽ không được gửi về.  
    > * *<thông tin bộ bài i>* có dạng giống ***[2]***

* *Play:<chỉ số người đánh>,<lá bài 1>,<lá bài 2>,...*
    > * Hiển thị hiệu ứng một người chơi đang đánh bài.
    > * *<chỉ số người đánh>* vẫn là 
    >   * 0 - bản thân
    >   * 1 - người kế trái
    >   * 2 - người đối diện
    >   * 3 - người kế phải
    > * *<lá bài i>* có dạng giống ***[1]***
* *UpdateMoney:<số tiền bị thay đổi 0>,<số tiền bị thay đổi 1>,...,<số tiền bị thay đổi 3>*
    > * Hiển thị hiệu ứng số tiền của các người chơi bị thay đổi (không cần hiển thị số tiền sau thay đổi)
    > * *<số tiền bị thay đổi i>* áp dụng cho người thứ i. Cách đánh chỉ số cho người chơi vẫn áp dụng như trên.
* *GameFinished:<chỉ số người thắng>*
    > * Hiển thị người chiến thắng, đồng thời loại bỏ hình ảnh của các bộ bài trên bàn, kết thúc game.
    > * *<chỉ số người thắng>* vẫn được áp dụng dựa trên cách đánh chỉ số cho người chơi.
* *Success:Play*
* *Success:Pass*
* *Failure:Play,\<reason>*
* *Failure:Pass,\<reason>*