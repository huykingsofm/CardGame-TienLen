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
        public CardSet[] Divive(int[] status){
            if (status.Count() != 4)
                throw new Exception("Input must be a 4-element-array");

            int NumberOfPlayer = status.CountDiff(0);

            if (NumberOfPlayer < 2 || NumberOfPlayer > 4)
                throw new Exception("Not support for {0} player(s)".Format(NumberOfPlayer));
               
            CardSet[] sets = new CardSet[4];
            
            this.list.Shuffle();
            
            for (int i = 0; i < 4; i++)
                if (status[i] != 0){
                    sets[i] = CardSet.Create((List<Card>)null);

                    for (int j = 0; j < 13; j++){
                        int value = this.list[i * 13 + j].ToInt();
                        sets[i].cards[value] = true;
                    }
                }
                else
                    sets[i] = null;

            return sets;
        }
    }
}