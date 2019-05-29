using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    class Deck : CardSet{
        /* 
         * Mục đích : Đại diện một bộ bài đầy đủ.
         * Thuộc tính : 
         *      + list : bộ bài dưới dạng danh sách.
         * Khởi tạo : 
         *      + Deck() : Hàm khởi tạo mặc định.
         * Phương thức :
         *       + Divide(Client[]) : Chia bài dựa trên các client đang chơi.
         */
        static public Deck __default__ = new Deck();
        private List<Card> list;
        public Deck() : base(){
            for (int i = 0; i < CardSet.MAX_CARDS; i++)
                this.cards[i] = true;
            list = this.ToList();
        }
        public CardSet[] Divive(Client[] players){
            int NumberOfPlayer = players.CountRealInstance();

            if (NumberOfPlayer < 2 || NumberOfPlayer > 4)
                throw new Exception("Not support for {0} player(s)".Format(NumberOfPlayer));
               
            CardSet[] sets = new CardSet[4];
            
            list.Shuffle();
            
            for (int i = 0; i < 4; i++)
                if (players[i] != null && players[i].IsLogin()){
                    sets[i] = CardSet.Create((List<Card>)null);

                    for (int j = 0; j < 13; j++){
                        int value = list[i * 13 + j].ToInt();
                        sets[i].cards[value] = true;
                    }
                }
                else
                    sets[i] = null;

            return sets;
        }
    }
}