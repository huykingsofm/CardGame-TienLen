using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    class Deck : CardSet{
        /* 
        # Mục đích : Là một bộ bài đầy đủ
        # Có khả năng chia bài
        */
        static public Deck __default__ = new Deck();
        public Deck() : base(){
            for (int i = 0; i < CardSet.MAX_CARDS; i++)
                this.cards[i] = true;
        }

        public CardSet[] Divive(int NumberOfPlayer = 4){
            if (NumberOfPlayer < 2 || NumberOfPlayer > 4)
                // throw new Exeption("Not support for " + NumberOfPlayer + " player(s)");
                return null;

            CardSet[] sets = new CardSet[NumberOfPlayer];
            
            List<Card> cardset = this.ToList();
            cardset.Shuffle();
            
            for (int i = 0; i < NumberOfPlayer; i++){
                sets[i] = new CardSet();
                for (int j = 0; j < 13; j++){
                    int value = cardset[i * 13 + j].ToInt();
                    sets[i].cards[value] = true;
                }
            }

            return sets;
        }
    }
}