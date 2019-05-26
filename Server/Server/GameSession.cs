using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;

namespace Server{
    public class GameSession : Session{
        /* 
        # Mục đích : Đại diện cho game dưới dạng một thực thể phản ứng nhanh.
        # 
        */
        Game game;
        ClientSession[] clientsessions;
        int[] index;
        double BetMoney;
        int TimeoutSignal;
        public override string Name => "GameSession";
        
        // RoomSession room;

        protected GameSession(ClientSession[] clients, Game game, double BetMoney) : base(){
            this.clientsessions = clients;
            this.game = game;
            this.BetMoney = BetMoney;
        }

        public static GameSession Create(ClientSession[] clients, Game game, double BetMoney){
            if (game == null)
                return null;
            
            if (clients.Count() != 4)
                return null;

            if (BetMoney < 0)
                return null;

            GameSession gamesession = new GameSession(clients, game, BetMoney);
            return gamesession;
        }

        public void UpdateForClients(){
            for(int i = 0; i < this.clientsessions.Count(); i++){
                if (this.clientsessions[i] != null){
                    this.Send(clientsessions[i], "GameInfo:{0}".Format(this.game.ToString(i) ) );
                }
            }
        }
        private void Timer(int time, int iplayer){
            this.TimeoutSignal = 0; // turn off
            int interval_time = time;
            time *= 1000;
            while(time > 0 && this.TimeoutSignal == 0){
                Thread.Sleep(interval_time);
                time -= interval_time;
            }
            if (this.TimeoutSignal != 0)
                this.Send(this, "Timeout:{0}".Format(iplayer));
        }
        public override void Solve(Object obj){
            Message message = (Message) obj;

            if (message.which != "Client" && message.which != "Game")
                throw new Exception("Game cannot solve message from " + message.who);

            switch(message.name){
                case "Play": 
                    /*
                    # Nhận thông tin về cách đánh bài của người chơi hiện tại
                    # .. Nếu thông tin này là của người khác, gửi thông báo đến
                    # .. người chơi đó và bỏ qua.
                    # Hành động cần làm : 
                    #       + Giải mã thông tin từ dạng 52 chuỗi thành 52 chữ số
                    #       + Tạo ra CardSet tương ứng với vector vừa được giải mã
                    #       + Gọi phương thức game.Play(CardSet)
                    #       + Kiểm tra kết quả trả về, nếu là null thì gửi trả thông
                    #         .. báo đến client. Ngược lại, tắt timer.
                    #       + Kiểm tra các thông tin trong kết quả trả về,
                    #         .. nếu có thông tin khác 0, gửi thông báo cho các client
                    #         .. cập nhật tiền. Nếu tất cả đều bằng 0 thì không gửi.
                    #       + Kiểm tra tiếp game đã kết thúc chưa, nếu kết thúc thì
                    #         .. gửi thông báo đến client, đóng session.
                    #       + Nếu game chưa kết thúc, gọi phương thức timer bắt đầu lượt kế tiếp.
                    #       + Nếu lượt kế tiếp là một người chơi ảo (đã đăng xuất) thì tự gọi pass.
                    */
                    break;
                case "Pass":
                    /*
                    # Nhận thông tin bỏ lượt của người chơi hiện tại.
                    # .. Nếu thông tin này là của người khác, gửi thông
                    # .. báo đến người đó và bỏ qua
                    # Hành động : + Gọi hàm pass và tắt timer hiện tại.
                    #             + Mở lại timer cho lượt kế tiếp.
                    #             + Nếu lượt kế tiếp là một người chơi ảo 
                    #               .. (đã đăng xuất) thì tự gọi pass
                    */
                    break;
                case "Timeout":
                    /*
                    # Nhận thông báo đã hết thời gian chơi của người chơi 
                    # .. hiện tại.
                    # Cần kiểm tra chỉ số của người chơi trong Timeout, nếu 
                    # .. khác với người chơi hiện tại thì bỏ qua.
                    # Thực hiện giống Message "Pass"
                    */
                    break;
                case "Signout":
                    /*
                    # Nhận thông báo đã thoát của người chơi
                    # Thiết lập giá trị ảo cho người chơi
                    # Khi đến lượt của người chơi đã thoát --> Pass
                    */
                    break;
                default:
                    Console.WriteLine("Cannot solve this message from Game : {0}".Format(message));
                    break;
            }
        }
        public override void Start(){
            this.UpdateForClients();
            base.Start();
        }
        public bool IsEnd(){
            return this.game.EndGameSignal;
        }
        public void WriteLog(){
            this.game.WriteLog();
        }
    }
}