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
        int BetMoney;
        int RemainTime;
        public override string Name => "GameSession";
        
        RoomSession room;

        protected GameSession(RoomSession room, ClientSession[] clients, Game game, int BetMoney) : base(){
            this.clientsessions = clients;
            this.game = game;
            this.BetMoney = BetMoney;
            this.room = room;
        }

        public static GameSession Create(RoomSession room, ClientSession[] clients, Game game, int BetMoney){
            if (game == null || room == null)
                return null;
            
            if (clients.Count() != 4)
                return null;

            if (BetMoney < 0)
                return null;

            GameSession gamesession = new GameSession(room, clients, game, BetMoney);
            return gamesession;
        }

        private void Timer(int time, int iplayer){
            this.RemainTime = time; // turn off
            int interval_time = 1000;
            time *= 1000;
            while(time > 0 && this.RemainTime == 0){
                Thread.Sleep(interval_time);
                time -= interval_time;
                this.RemainTime -= 1;
            }
            if (this.RemainTime == 0)
                this.Send(this, "Timeout:{0}".Format(iplayer));
        }
        public override void Solve(Object obj){
            Message message = (Message) obj;

            if (message.which != "ClientSession")
                throw new Exception("Game cannot solve message from " + message.who);

            switch(message.name){
                case "Play":{
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
                    int index = this.clientsessions.FindById(message.id); 
                    if (index == -1) {
                        this.WriteLine("This message must be come from client");
                        return;
                    }

                    if (index != this.game.whoturn){
                        this.WriteLine("This is turn of player[{0}]", this.game.whoturn);
                        return;
                    }
                    CardSet cardset = null;
                    try{
                        cardset = CardSet.Create(message.args);
                    }
                    catch(Exception e){
                        this.WriteLine(e.Message);
                        return;
                    }

                    List<int> res = null;
                    try{
                        res = this.game.Play(cardset);
                    }
                    catch(Exception e){
                        this.Send(this.clientsessions[index], "Failure:Play,{0}".Format(e.Message));
                        return;
                    }

                    this.Send(this.clientsessions[index], "Success:Play");
                    
                    string playingmessage = "PlayingCard:{0},{1}".
                        Format(index, String.Join(',', message.args));
                    this.Send(this.room, playingmessage);

                    this.Send(this.room, "UpdateGame");
                    
                    int[] coef_money = res.ToArray().Take(0, -2);
                    this.UpdateMoneyForClients(coef_money);

                    if (res.Last() != -1){
                        // Game đã kết thúc
                        this.Send(this.room, "GameFinished:{0}".Format(res.Last()));
                    }
                    break;
                }
                case "Pass":{
                    /*
                    # Nhận thông tin bỏ lượt của người chơi hiện tại.
                    # .. Nếu thông tin này là của người khác, gửi thông
                    # .. báo đến người đó và bỏ qua
                    # Hành động : + Gọi hàm pass và tắt timer hiện tại.
                    #             + Mở lại timer cho lượt kế tiếp.
                    #             + Nếu lượt kế tiếp là một người chơi ảo 
                    #               .. (đã đăng xuất) thì tự gọi pass
                    */
                    int index = this.clientsessions.FindById(message.id); 
                    if (index == -1) {
                        this.WriteLine("This message must be come from client");
                        return;
                    }

                    if (index != this.game.whoturn){
                        this.WriteLine("This is turn of player[{0}]", this.game.whoturn);
                        return;
                    }

                    List<int> ret = null;
                    CardSet moveset = null;
                    try{
                        ret = this.game.Pass(ref moveset);
                    }
                    catch(Exception e){
                        this.Send(this.clientsessions[index], "Failure:Pass,{0}".Format(e.Message));
                        return;
                    }

                    this.Send(this.clientsessions[index], "Success:Pass");

                    if (ret != null){
                        int[] coef_money = ret.ToArray().Take(0, - 2);
                        this.UpdateMoneyForClients(coef_money); 
                    }

                    if (moveset != null){
                        string playingmessage = "PlayingCard:{0},{1}".
                            Format(index, moveset.ToString(sum:false));
                        this.Send(this.room, playingmessage);
                    }

                    this.Send(this.room, "UpdateGame");
                    
                    if (ret != null && ret.Last() != -1)
                        this.Send(this.room, "GameFinished:{0}".Format(ret.Last()));
                    break;
                }
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

        public void UpdateMoneyForClients(int[] coef_money){
            if (coef_money.IsAll(0) == false){
                int sum = 0;
                int receiver = -1;
                for (int i = 0; i < coef_money.Count(); i++)
                    if (this.clientsessions[i] != null){
                        int loss = 0;
                        coef_money[i] = coef_money[i] * this.BetMoney;
                        if (coef_money[i] < 0)
                            loss = this.clientsessions[i].client.user.ChangeMoney(coef_money[i]);
                        else if(coef_money[i] > 0)
                            receiver = i;
                        sum -= loss;
                    }
                coef_money[receiver] = (int) (0.9 * sum);
                this.clientsessions[receiver].client.user.ChangeMoney(coef_money[receiver]);

                for (int i = 0; i < coef_money.Count(); i++)
                    if (this.clientsessions[i] != null){
                        List<int> updatemoney = new List<int>();
                                
                        for (int add = 0; add < coef_money.Count(); add++){
                            int newindex = (i + add) % coef_money.Count();
                            updatemoney.Add(coef_money[newindex]);
                        }

                        this.clientsessions[i].client.user.GetInfo();
                        this.Send(this.clientsessions[i], "UpdateMoney:{0}"
                            .Format(String.Join(',', updatemoney)));
                    }
                        
                this.Send(this.room, "UpdateRoom");
            }

        }
        public void UpdateForClients(){
            for(int i = 0; i < this.clientsessions.Count(); i++){
                if (this.clientsessions[i] != null){
                    this.Send(clientsessions[i], "GameInfo:{0}".Format(this.GameInfo(i) ) );
                }
            }
        }
        public bool IsEnd(){
            return this.game.EndGameSignal;
        }
        public void WriteLog(){
            this.game.WriteLog();
        }

        public string GameInfo(int index){
            string ret = "{0},{1}".Format(this.game.ToString(index), this.RemainTime);
            return ret;
        }
        public string OnTableInfo(){
            return this.game.OnTableInfo();
        }
    }
}