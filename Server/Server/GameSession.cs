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
         * Mục đích : Đại diện cho game dưới dạng một thực thể phản ứng nhanh.
         * 
         */
        private Game game;
        private ClientSession[] clientsessions;
        private int BetMoney;
        private bool TimerStop;
        private bool AIStop;
        public override string Name => "GameSession";
        private Thread timerthread;
        private Thread AIThread;
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
        private void Timer(){
            this.TimerStop = false; // turn off
            int interval_time = 1000;
            int time = Game.TIMEOUT;
            int iplayer = this.game.whoturn;
            while(time > 0 && this.TimerStop == false){
                Thread.Sleep(interval_time);
                time -= (int)(interval_time / 1000);
                this.Send(this, "Time:{0},{1}".Format(iplayer, time) );
            }
            if (this.TimerStop == false)
                this.Send(this, "Timeout:{0}".Format(iplayer));
        }
        private Thread StartTimer(){
            this.StopTimer();
            this.timerthread = new Thread(this.Timer);
            this.timerthread.Start();
            return this.timerthread;
        }
        private void StopTimer(){
            this.TimerStop = true;
            while (this.timerthread != null && this.timerthread.IsAlive == true){
                Thread.Sleep(300);
                this.TimerStop = true;
            }
        }
        private void WaitForAI(){
            this.AIStop = false;
            int index = -1;
            while (this.AIStop == false){
                while (this.game.GetStatus(this.game.whoturn) != Room.AI 
                && this.game.GetStatus(this.game.whoturn) != Room.AFK
                && this.AIStop == false){
                    Thread.Sleep(300);
                    if (this.game.whoturn != index)
                        index = -1;
                }

                if (this.AIStop == true ||  this.game.EndGameSignal)
                    break;
                
                if (this.game.whoturn == index)
                    continue;

                index = this.game.whoturn;               

                if (this.game.GetStatus(index) == Room.AI){
                    try{
                        CardSet cardset = this.game.GetMoveFromAI();
                        this.Send(this, "AIPlay:{0},{1}".Format(index, cardset.ToString(sum:false)));
                    }
                    catch{
                        this.Send(this, "AIPlay:{0}".Format(index));
                    }
                }
                else if (this.game.GetStatus(index) == Room.AFK){
                    this.Send(this, "AutoPass:{0}".Format(index));
                }
                Thread.Sleep(300);
            }
        }
        private void StartWaitForAI(){
            this.AIThread = new Thread(this.WaitForAI);
            this.AIThread.Start();
        }
        private void StopWaitForAI(){
            this.AIStop = true;
            while(this.AIThread.IsAlive == true){
                Thread.Sleep(300);
                this.AIStop = true;
            }
        }
        public override void Solve(Object obj){
            Message message = (Message) obj;

            if (message.which != "ClientSession" && message.id != this.id)
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
                    this.Play(index, cardset);
                    break;
                }
                case "AIPlay":{
                    if (message.id != this.id) {
                        this.WriteLine("This message must be come from game");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false){
                        this.WriteLine("Parameter is incorrect");
                        return;
                    }
 
                    if (index != this.game.whoturn){
                        this.WriteLine("This is turn of player[{0}]", this.game.whoturn);
                        return;
                    }
                    CardSet cardset = null;
                    try{
                        cardset = CardSet.Create(message.args.Take(1, -1));
                    }
                    catch(Exception e){
                        this.WriteLine(e.Message);
                        this.Pass(index);
                        return;
                    }

                    try{
                        if(this.Play(index, cardset) == false)
                            throw new Exception();
                    }
                    catch{
                        this.Pass(index);
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

                    this.Pass(index);
                    break;
                }
                case "AutoPass":{
                    if (message.id != this.id) {
                        this.WriteLine("This message must be come from game");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false){
                        this.WriteLine("Parameter is incorrect");
                        return;
                    }
 
                    if (index != this.game.whoturn){
                        this.WriteLine("This is turn of player[{0}]", this.game.whoturn);
                        return;
                    }
                    
                    this.Pass(index);
                    break;
                }
                case "Timeout":{
                    /*
                    # Nhận thông báo đã hết thời gian chơi của người chơi 
                    # .. hiện tại.
                    # Cần kiểm tra chỉ số của người chơi trong Timeout, nếu 
                    # .. khác với người chơi hiện tại thì bỏ qua.
                    # Thực hiện giống Message "Pass"
                    */
                    
                    if (this.id != message.id) {
                        this.WriteLine("This message must be come from Game");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 1){
                        this.WriteLine("Message must have a parameter");
                        return;
                    }

                    int index = 0;
                    if (Int32.TryParse(message.args[0], out index) == false){
                        this.WriteLine("Parameter is incorrect");
                        return;
                    }

                    this.Timeout(index);
                    break;
                }
                case "Time":{
                    if (this.id != message.id){
                        this.WriteLine("Message must come from Game");
                        return;
                    }

                    if (message.args == null || message.args.Count() != 2){
                        this.WriteLine("Message must have 2 parameters");
                        return;
                    }

                    int iplayer = 0;
                    int remaintime = 0;

                    if (Int32.TryParse(message.args[0], out iplayer) == false ||
                    Int32.TryParse(message.args[1], out remaintime) == false){
                        this.WriteLine("Parameters is incorrect");
                        return;
                    }

                    if (iplayer != this.game.whoturn){
                        this.WriteLine("This is turn of {0} but {1}".Format(this.game.whoturn, iplayer));
                        return;
                    }

                    if (remaintime < 0 || remaintime > Game.TIMEOUT){
                        this.WriteLine("Time is out of range");
                        return;
                    }

                    for (int i = 0; i < this.clientsessions.Count(); i++)
                        if (this.clientsessions[i] != null){
                            int index = (4 + iplayer - i) % 4;
                            this.Send(this.clientsessions[i], "Time:{0},{1}".
                                Format(index, message.args[1]));
                        }

                    break;
                }
                case "Disconnect":{

                    /*
                    # Nhận thông báo đã thoát của người chơi
                    # Thiết lập giá trị ảo cho người chơi
                    # Khi đến lượt của người chơi đã thoát --> Pass
                    */
                    break;
                }
                default:{
                    Console.WriteLine("Cannot solve this message from Game : {0}".Format(message));
                    break;
                }
            }
        }
        private bool Play(int index, CardSet cardset){
            if (index != this.game.whoturn)
                throw new Exception (
                    "This is turn of player[{0}] not {1}".Format(this.game.whoturn, index)
                );
            lock(this){
                List<int> res = null;
                try{
                    res = this.game.Play(cardset);
                }
                catch(Exception e){
                    if (this.clientsessions[index] != null) 
                        this.Send(this.clientsessions[index], "Failure:Play,{0}".Format(e.Message));
                    else
                        this.WriteLine("Failure:Play,{0}".Format(e.Message));
                    return false;
                }

                this.StopTimer();
                this.Update(index, res, "Play", cardset.ToString(sum:false));
                return true;
            }
        }        
        private void Pass(int index){
            if (index != this.game.whoturn)
                throw new Exception("This is turn of {0} not {1}".Format(this.game.whoturn, index));

            lock(this){
                List<int> ret = null;
                CardSet moveset = null;
                try{
                    ret = this.game.Pass(out moveset);
                }
                catch(Exception e){
                    if (this.clientsessions[index] != null) 
                        this.Send(this.clientsessions[index], "Failure:Pass,{0}".Format(e.Message));
                    else
                        this.WriteLine("Failure:Pass,{0}".Format(e.Message));
                    return;
                }

                this.StopTimer();
                string ms = moveset == null ? null : moveset.ToString(sum:false);
                this.Update(index, ret, "Pass", ms);
            }
        }
        private void Timeout(int index){
            lock(this){
                if (this.clientsessions[index] == null){
                    this.WriteLine("This client is not exist in game");
                    return;
                }

                if (index != this.game.whoturn){
                    this.WriteLine("This is turn of player[{0}]", this.game.whoturn);
                    return;
                }

                List<int> ret = null;
                CardSet moveset = null;
                try{
                    ret = this.game.Pass(out moveset);
                }
                catch(Exception e){
                    if (this.clientsessions[index] != null) 
                        this.Send(this.clientsessions[index], "Failure:Pass,{0}".Format(e.Message));
                    else
                        this.WriteLine("Failure:Pass,{0}".Format(e.Message));
                    return;
                }

                string ms = moveset == null ? null : moveset.ToString(sum:false);
                this.Update(index, ret, "Pass", ms);
            }
        }
        private void Update(int index, List<int> ret, string request, string moveset){
            if (this.clientsessions[index] != null) 
                    this.Send(this.clientsessions[index], "Success:{0}".Format(request));
                else
                    this.WriteLine("Success:{0}".Format(request));
            

            if (moveset != null){
                string playingmessage = "PlayingCard:{0},{1}".
                    Format(index, moveset);
                this.Send(this.room, playingmessage);
            }

            if (ret != null){
                int[] coef_money = ret.ToArray().Take(0, - 2);
                this.UpdateMoneyForClients(coef_money); 
            }

            this.Send(this.room, "UpdateGame");
                    
            if (ret != null && ret.Last() != -1){
                this.StopWaitForAI();
                this.Send(this.room, "GameFinished:{0}".Format(ret.Last()));
            }
            else
                this.StartTimer();
        }
        public override void Start(){
            this.UpdateForClients();
            base.Start();
            this.StartTimer();
            this.StartWaitForAI();
        }
        public override void Stop(string mode = "normal"){
            this.StopTimer();
            base.Stop(mode);
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

                        if (receiver == i)
                            sum += loss;
                        sum -= loss;
                    }

                if (receiver != -1)
                    coef_money[receiver] = (int) (0.9 * sum);
    
                if(receiver != -1 && this.clientsessions[receiver] != null)   
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
        public void WriteLog(){
            this.game.WriteLog();
        }
        public override void Destroy(string mode = "normal"){
            this.WriteLog();
            base.Destroy(mode);
        }
        public string GameInfo(int index){
           return this.game.ToString(index);
        }
        public string OnTableInfo(){
            return this.game.OnTableInfo();
        }
    }
}