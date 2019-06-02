using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Server{
    public class Game : Thing{
        /*
         * Mục đích : Lưu trữ và thực thi một game tiến lên.
         * Hoạt động : + Khi khởi tạo cần biết số lượng người chơi
         *               .. và người chơi bắt đầu.
         *             + Sau khi khởi tạo có thể bắt đầu chơi với 2
         *               .. phương thức là Play(CardSet) và Pass().
         *             + Mỗi phương thức đều thực hiện cho người chơi hiện
         *               .. tại cần đánh bài. 
         *             + Mỗi khi gọi Play() hoặc Pass() thành công, lượt
         *               .. sẽ chuyển cho người tiếp theo.
         *             + Game sẽ kết thúc khi bất kỳ người chơi nào hết bài.
         *             + Có thể lưu trữ nhật ký khi kết thúc game.
        */

        public override string Name => "Game";
        public const int TIMEOUT = 30;          //(second)
        public const string DefaultLogDir = "../GameLog/";      // Thư mục lưu các nhật ký
        private CardSet[] initilization;        // Lưu lại bộ bài khởi tạo của mỗi người chơi
        private CardSet[] cards;                // Lưu bộ bài hiện tại của mỗi người chơi.
        private Client[] players;               // Lưu thông tin người chơi
        private int Starter;                    // Người chơi thực hiện nước đi đầu tiên
        private List<Move> HistoryMove;         // Lưu lịch sử nước đi.
        private List<List<int>> HistoryResult;  // Lịch sử kết quả
        public int whoturn{get; private set;}   // Lưu chỉ số của người hiện tại cần đánh bài.
        private Move lastmove;                  // Nước đi cuối cùng được đánh ra.
        private int lastplayer;                 // Chỉ số của người chơi cuối cùng đánh bài.
        private int SpecialChain;               // = 0 nếu không có gì, > 0 nếu có chuỗi chặn heo.
        private Card smallestcard = null;       // lá bài nhỏ nhất mà người chơi hiện tại cần phải đánh
        private List<CardSet> onboardsets = null;           // lưu lại những bộ bài đang ở trên bàn
        public bool EndGameSignal {get; private set;}       // Tín hiệu kết thúc game.
        public string LogDir;                   // các file nhật ký game được lưu ở đây
        public int[] PlayerStatus{get; private set;}             /* trạng thái các player trong game
                                                 * Sử dụng lại các trạng thái ở lớp Room
                                                 */

        private Game(Client[] players, int[] status, int Starter, string dir){
            int NumberOfPlayer = status.CountDiff(Room.NOT_IN_ROOM);
            this.players = (Client[])players.Clone();
            this.PlayerStatus = (int[])status.Clone();

            this.initilization = Deck.__default__.Divive(status);
            this.cards = new CardSet[4];
            
            for (int i = 0; i < this.players.Count(); i++){
                if (this.initilization[i] != null)
                    this.cards[i] = this.initilization[i].Clone();
            }

            if (Starter == -1){
                /*
                 * Find smallest card to known who have first turn
                 */
                for (int i = 0; i < this.players.Count(); i++)
                    if (this.cards[i] != null){
                        Card tmp = this.cards[i].FindSmallest();
                        if (this.smallestcard == null || this.smallestcard.IsLarger(tmp)){
                            this.smallestcard = tmp;
                            Starter = i;
                        }
                    }     
            }

            this.LogDir = dir.TrimEnd(new char[]{'/', '\\'}) + "/";

            this.HistoryMove = new List<Move>();
            this.HistoryResult = new List<List<int>>();
            this.onboardsets = new List<CardSet>();
            this.whoturn = Starter;
            this.lastplayer = Starter;
            this.Starter = Starter;
            this.lastmove = null;
            this.SpecialChain = 0;
            this.EndGameSignal = false;
        }
        static public Game Create(
            Client[] players, 
            int[] status,
            int Starter = 0, 
            string dir = Game.DefaultLogDir
            ){

            if (players == null || status == null)
                throw new Exception("Parameters cannot null instances");
            
            if (players.Count() != 4)
                throw new Exception("Game need 4-users array to initialize (it can have null elements)");

            int NumberOfPlayer = status.CountDiff(Room.NOT_IN_ROOM);

            if (NumberOfPlayer > 4 || NumberOfPlayer < 2)
                throw new Exception("Game need at least 2 users or at most 4 users to play");
            
            if (Starter != -1 && (Starter < 0 || Starter >= NumberOfPlayer))
                throw new Exception("Starter must be a index between 0 and 3");

            if (Starter != -1 && players[Starter] == null)
                throw new Exception("Starter can be a null instance");

            if (players.Count() != status.Count())
                throw new Exception("Player and status are not synchronized");

            for (int i = 0; i < players.Count(); i++)
                if (status[i] == Room.NOT_IN_ROOM && players[i] != null)
                    throw new Exception("Player and status are not synchronized");

            Game game = new Game(players, status, Starter, dir);
            return game;
        }

        public CardSet GetMoveFromAI(string aipath = null){
            string fullpath = Utils.GetPathOfThis() + @"\..\..\Game\";
            fullpath = @"C:\HOCTAP\LT_MANG\CardGame-TienLen\Server\Game\";
            if (aipath == null)
                aipath = fullpath + "AI.exe";

            aipath = @"C:\HOCTAP\LT_MANG\CardGame-TienLen\Server\AI\bin\Debug\netcoreapp2.2\win10-x64\AI.exe";

            string name = DateTime.Now.Ticks.ToString();
            string inputfile = fullpath + name + ".inp";
            string outputfile = fullpath + name + ".out";

            // Tạo file đầu vào trước khi thực thi AI
            using(var f = new StreamWriter(inputfile)){
                int index = this.whoturn;
                f.WriteLine(this.cards[this.whoturn].ToVector());

                for (int add = 1; add < this.players.Count(); add++){
                    int i = (index + add) % this.players.Count();
                    if (this.cards[i] == null)
                        f.WriteLine("0");
                    else
                        f.WriteLine(this.cards[i].Count().ToString());    
                }
            }

            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = @aipath,
                    Arguments = "{0} {1}".Format(inputfile, outputfile),
                    UseShellExecute = false, RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
 
            process.Start();
            process.WaitForExit();

            using(var f = new StreamReader(outputfile)){
                string content = f.ReadLine();
                CardSet tmp = null;
                try{
                    tmp = CardSet.Create(arr:content.Split(" "), format:"vector");
                }
                catch(Exception e){
                    this.WriteLine(e.Message);
                    return this.cards[this.whoturn].RandomMove();
                }
                return tmp;
            }
        }

        public List<int> Play(CardSet moveset){ 
            /*
            # Mục đích : thực hiện đánh nước bài với player đang trong lượt
            # Trả về : + null nếu không thể đánh được với nước bài này
            #          + Trả về một list<int> nếu có thể thực hiện được nước đi
            #            .. list<int> bao gồm n + 1 thành phần, với n là số lượng
            #            .. người chơi.
            #            .. Các thành phần từ [0..n-1] là hệ số tiền sẽ bị thay đổi 
            #            .. của n người chơi sau khi người chơi hiện tại đánh nước 
            #            .. bài này. Thông thường sẽ là 0 (tương ứng với không thay
            #            .. đổi), khác 0 nếu có chuỗi chặn heo.
            #            .. Thành phần thứ n là trạng thái của game : 0 - nếu game
            #            .. còn tiếp tục và 1 nếu game kết thúc.
            */

            if (this.EndGameSignal)
                throw new Exception("Game finished");

            // Khởi tạo nước đánh với player lượt này
            Server.Move move = Move.Create(this.cards[whoturn], moveset);
            if (move == null) // Nước đánh không thể thực hiện với player này
                throw new Exception("This move is not compatible with player");

            // Kiểm tra nước đi này có phù hợp với nước đi trước đó không
            int iStatus = move.IsValid(lastmove);
            
            if (iStatus == 0) // Nước đánh không phù hợp với nước đánh trước đó
                throw new Exception("This move is not compatible with previous move");
            
            // Kiểm tra nước đánh đầu tiên, phải đánh có lá nhỏ nhất
            // Chỉ dành cho người được đánh ưu tiên
            if (this.smallestcard != null 
            && moveset.Count(this.smallestcard.number, this.smallestcard.suit) == 0)
                throw new Exception("You must move with smallest card");
            else
                this.smallestcard = null;

            // Nếu không còn lỗi nào, cho phép người chơi thực hiện nước đánh này
            this.cards[whoturn].Move(moveset);

            // Khởi tạo trước các giá trị trả về - hệ số tiền thay đổi sau nước đi, mặc định tất cả là 0
            List<int> ret = new List<int>();
            for(int i = 0; i < this.players.Count(); i++)
                ret.Add(0);

            // Tạo tín hiệu kết thúc, ban đầu là -1 - game chưa kết thúc
            ret.Add(-1);

            // Số lượng lá bài của người chơi hiện tại đã hết
            // --> Game kết thúc
            if (this.cards[this.whoturn].Count() == 0){ 
                // Đặt tín hiệu kết thúc game, ret.Last == chỉ số người thắng
                this.EndGameSignal = true;
                ret[ret.Count() - 1] = this.whoturn; 

                /*
                # + Tính lại hệ số tiền phải thay đổi của mỗi người chơi
                # + Công thức tính hệ số tiền trả của người thua là : 
                #   .. heso =  số lượng lá bài còn trên tay
                #            + số lượng lá 2 trên tay
                #            + số lượng lá 2 cơ và rô trên tay
                #   .. tương đương với lá 2 đen có hệ số 2, lá 2 đỏ có hệ số 3
                # + Công thức tính hệ số tiền nhận được của người thắng là :
                #   .. heso = tổng lượng tiền phải trả của người thua * 0.9
                */

                // Khởi tạo biến lưu tổng hệ số của người thắng
                double sum = 0;
                for (int i = 0; i < this.cards.Count(); i++)
                    if (this.cards[i] != null){
                        ret[i] -= this.cards[i].Count();
                        ret[i] -= this.cards[i].Count(number:2);
                        ret[i] -= this.cards[i].Count(number:2, suit:2); 
                        ret[i] -= this.cards[i].Count(number:2, suit:3);
                        sum += ret[i];
                    }
                ret[this.whoturn] = (int) (- sum * 0.9);
            }

            /*
            # + Tính hệ số tiền thay đổi sau khi kết thúc nước đánh (chủ yếu 
            #   .. là vì chuỗi chặn heo).
            # + Công thức :
            #       - Nếu người A đánh ra heo, người B chặn heo bằng tứ quý
            #         .. hoặc đôi thông, người A sẽ bị trừ từ 2 - 3 lần tiền
            #         .. cược, người B sẽ nhận 90% số tiền người A bỏ ra.
            #       - Nếu sau đó người C dùng đôi thông lớn hơn, hoặc một nước
            #         .. bài nào đó, mà lớn hơn nước bài vừa đánh của người B,
            #         .. thì người B sẽ bị trừ gấp đôi số tiền mà người A bỏ ra
            #         .. (tức 4-6 lần tiền cược), người C sẽ nhận được 90% số 
            #         .. tiền mà người B bỏ ra.
            #       - Nếu tiếp tục chặn, người bị chặn sẽ bị trừ số tiền gấp đôi
            #         .. người trước đó bỏ ra, tức tiếp tục là 8-12, 16-24 lần 
            #         .. tiền cược, tùy thuộc vào lá heo của người A đánh ra, người
            #         .. chặn sẽ được 90% số tiền người bị chặn bỏ ra.
            #       - Chuỗi chặn kết thúc khi lượt đánh này kết thúc, tức 3 người 
            #         .. còn lại đều bỏ lượt.
            # + Lưu ý : Chuỗi chặn chỉ xảy ra khi bắt đầu chặn heo, nếu chỉ đơn giản
            #           .. là bắt đôi thông hoặc tứ quý bằng nước đánh cao hơn, sẽ 
            #           .. không bị thay đổi tiền.  
            */

            if (iStatus == 2) { 
                // Bắt đầu chuỗi chặn heo
                // Kiểm tra lá heo của người bị chặn là chất gì
                Card dummy = Card.Create(lastmove.values[0]);
                if (dummy.suit == Card.SUIT_CLUBS || dummy.suit == Card.SUIT_SPADES)
                    this.SpecialChain = 2; // Chất chuồng hoặc bích
                else
                    this.SpecialChain = 3; // Chất rô hoặc cơ

                ret[this.lastplayer] += -this.SpecialChain;
                ret[this.whoturn] += (int) (this.SpecialChain * 0.9);
                this.SpecialChain *= 2;
            }
            
            if ((iStatus == 1 || iStatus == 3) && this.SpecialChain > 0){ 
                // Nếu đang có chuỗi chặn heo
                ret[whoturn] += this.SpecialChain;
                ret[lastplayer] += (int)(-this.SpecialChain * 0.9);
                this.SpecialChain *= 2;
            }

            // Thêm nước đánh vào lịch sử
            // Thay đổi các thành phần cần thiết
            this.HistoryMove.Add(move);
            this.lastmove = move;
            this.lastplayer = this.whoturn;
            this.onboardsets.Add(move.GetMoveSet());
            this.Next();

            this.HistoryResult.Add(ret);
            return ret;
        }
        private void Next(){
            this.whoturn = (this.whoturn + 1) % this.cards.Count();
            while(this.cards[this.whoturn] == null) 
                this.whoturn = (this.whoturn + 1) % this.cards.Count();
        }
        public List<int> Pass(out CardSet moveset){
            /*
            # Người chơi hiện tại yêu cầu bỏ lượt
            # Nhưng nếu lượt chơi bắt buộc người này thực hiện,
            # --> phải thực hiện nước đi ngẫu nhiên
            # Trường hợp trên xảy ra khi người này là người
            # .. bắt đầu của lượt này
            */
            if (this.EndGameSignal)
                throw new Exception("Game finished");

            List<int> ret = null;
            moveset = null;

            if (this.lastmove == null){
                // Bắt buộc đánh ngẫu nhiên
                CardSet random;
                if (this.smallestcard == null)
                    random = this.cards[this.whoturn].RandomMove();
                else
                    random = CardSet.Create( (new List<Card>( new Card[]{this.smallestcard} ) ) );

                ret = this.Play(random);
                moveset = random;
            }
            else{
                this.Next();
                this.HistoryMove.Add(null);
                this.HistoryResult.Add(null);
            }

            if (this.whoturn == this.lastplayer){ 
                // Hết lượt
                this.lastmove = null;
                this.SpecialChain = 0;
                this.onboardsets = new List<CardSet>();
            }
            return ret;
        }
        public int GetStatus(int index){
            return this.PlayerStatus[index];
        }
        public string ToString(int index){
            /*
             * GameInfo: (dưới góc nhìn của người ở vị trí index)
             *      + Bộ bài của người chơi hiện tại.
             *      + Số lượng bài của người chơi khác.
             *      + Chỉ số người chơi đang tới lượt.
             *      + Khả năng bỏ lượt (-1 : bỏ qua, 0 : không thể bỏ lượt, 1 : có thể bỏ lượt).
             */
            if (index < 0 || index > this.players.Count())
                throw new Exception("User[{0}] is not exist in game".Format(index));

            List<string> arr = new List<string>();

            // Thông tin của người chơi thứ index
            if (this.cards[index] == null)
                arr.Add("0");
            else
                arr.Add(this.cards[index].ToString());

            int onturn = 0;
            for (int add = 1; add < this.players.Count(); add++){
                int i = (index + add) % this.players.Count();
                if (this.cards[i] == null)
                    arr.Add("0");
                else
                    arr.Add(this.cards[i].Count().ToString());
                
                if (i == this.whoturn)
                    onturn = add;
            }

            arr.Add(onturn.ToString());
            
            int pass = this.whoturn == index ? 0 : -1;
            pass = (pass == 0) && this.whoturn == this.lastplayer ? 1 : pass;
            arr.Add(pass.ToString());

            string ret = String.Join(",", arr);
            return ret;
        }
        public string OnTableInfo(){
            /* 
             * MovedCardSets:
             *      + Số lượng bộ bài trên bàn ở lượt hiện tại.
             *      + Các bộ bài trên bàn ở lượt hiện tại.
             */

            List<string> ret = new List<string>();

            ret.Add(this.onboardsets.Count().ToString());

            foreach(var cardset in this.onboardsets)
                ret.Add(cardset.ToString());
            
            return String.Join(',', ret);
        }
        public void WriteLog(){
            /*
            # Mục đích : Viết lịch sử lượt đánh ra 2 file trong 1 folder
            #            .. được tạo ngẫu nhiên, tên folder được lưu lại ở
            #            .. file logname.log ở LogDir.
            #            .. Hai file lần lượt là ".init" và ".move".
            # Các mục cần viết là bộ bài ban đầu của mỗi người chơi
            # .. và nước đánh mỗi lượt.
            # Mỗi thể hiện của bộ bài hoặc nước đánh sẽ được ghi
            # .. trên một dòng, gồm 52 phần tử one-hot.
            */
            if (this.EndGameSignal == false)
                throw new Exception("Game has not already finished");

            Directory.CreateDirectory(this.LogDir);

            // Generate random folder name which contains 2 log files
            string GameLogDirName = DateTime.Now.Ticks.ToString();

            if (File.Exists(this.LogDir + "logname.log") == false){
                File.Create(this.LogDir + "logname.log");
            }

            // Write above folder name to file "logname.log"
            using(var f = new StreamWriter(this.LogDir + "logname.log", append:true)){
                f.WriteLine(GameLogDirName);
            }

            string dir = this.LogDir + GameLogDirName;
            Directory.CreateDirectory(dir);

            string InitPath = dir + "/.init"; 
            string MovePath = dir + "/.move";
            string ResPath  = dir + "/.res";

            using (var f = new StreamWriter(InitPath, append:true)){
                f.WriteLine(this.Starter);

                foreach(CardSet tmp in this.initilization)
                    if(tmp != null)
                        f.WriteLine(tmp.ToVector());
                    else
                        f.WriteLine(CardSet.Create((List<Card>) null).ToVector());
            }

            using (var f = new StreamWriter(MovePath, append:true)){
                foreach(Move tmp in this.HistoryMove){
                    string str = "";
                    if (tmp != null)
                        str = tmp.GetMoveSet().ToVector();
                    else
                        str = CardSet.Create((List<Card>) null).ToVector();
                    f.WriteLine(str);
                }
            }
            using (var f = new StreamWriter(ResPath, append:true)){
                foreach(var tmp in this.HistoryResult){
                    string str = "";
                    if (tmp != null)
                        str = tmp.ToArray().Take(0, -2).ToStringE();
                    else
                        str = new int[]{0, 0, 0, 0}.ToStringE();
                    f.WriteLine(str);
                }
            }
        }

        public CardSet[] GetInitCardSets(){
            CardSet[] tmp = new CardSet[4];
            for (int i = 0; i < 4; i++)
                if (this.initilization[i] != null)
                    tmp[i] = this.initilization[i].Clone();

            return tmp;
        }

    }
}