using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class CardSet : Object{
        /*
        # Mục đích : Chứa thông tin của một tập bài
        # Lưu trữ : Là một vector one-hot gồm 52 phần tử, mỗi phần tử đại diện cho
        #           .. một quân bài, bắt đầu là 3 bích -> 3 chuồng ->...-> 2 Cơ
        # Hành vi : + CardSet có một hành vi là Move(đánh bài)
        */
        public const int MAX_CARDS = 52;
        public bool[] cards{get; protected set;}
        public CardSet(){
            this.cards = new bool[MAX_CARDS];
            for (int i = 0; i < CardSet.MAX_CARDS; i++)
                this.cards[i] = false;
        }
        public static CardSet Create(List<Card> list){
            CardSet set = new CardSet();
            foreach(Card card in list){
                int value = card.ToInt();
                set.cards[value] = true;
            }
            if (set.Count() == list.Count())
                return set;
            return null;
        }
        public List<Card> ToList(){
            List<Card> tmp = new List<Card>();
            
            for (int i = 0; i < CardSet.MAX_CARDS; i++){
                if (this.cards[i])
                    tmp.Add(Card.Create(i));
            }

            return tmp;
        }
        public int Count(){
            int count = 0;
            foreach(bool card in this.cards){
                if (card)
                    count++;
            }
            return count;
        }
        public int Count(int number){
            // Đếm số lượng quân bài có số nhất định
            int count  = 0;
            for(int i = 0; i < CardSet.MAX_CARDS; i++){
                if (this.cards[i]){
                    int number_t = Card.Create(i).number;
                    if (number_t == number)
                        count++;
                }
            }
            return count;
        }
        public int Count(int number, int suit){
            // Đếm số lượng quân bài có số nhất định
            int count  = 0;
            for(int i = 0; i < CardSet.MAX_CARDS; i++){
                if (this.cards[i]){
                    Card card = Card.Create(value : i);
                    if (card.number == number && card.suit == suit)
                        count++;
                }
            }
            return count;
        }
        public bool Move(CardSet moveset){
            Server.Move move = Server.Move.Create(this, moveset);
            
            if (move == null) // Nếu nước đánh không hợp lệ
                return false;

            List<Card> movelist = moveset.ToList();
            foreach(Card card in movelist){
                int value = card.ToInt();
                this.cards[value] = false;
            }

            return true;
        }
        public bool Move(List<Card> list){
            CardSet moveset = CardSet.Create(list);
            return this.Move(moveset);
        }
        public CardSet RandomMove(){
            List<Card> list = new List<Card>();
            List<Card> dummy = this.ToList();
            if (dummy.Count() < 0)
                return null;
            list.Add(dummy.First());
            
            return CardSet.Create(list);
        }
        public override string ToString(){
            string str = "";
            foreach(bool card in this.cards)
                str += (card ? 1 : 0) + " ";
            str.Trim();
            return str;
        }
        public CardSet Clone(){
            CardSet dummy = new CardSet();
            for (int i = 0; i < CardSet.MAX_CARDS; i++)
                dummy.cards[i] = this.cards[i];
            return dummy;
        }
    }
}