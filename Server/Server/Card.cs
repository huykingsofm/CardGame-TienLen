using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Card : Object{
        public const int SUIT_SPADES = 0; // Bích
        public const int SUIT_CLUBS = 1; // Chuồng
        public const int SUIT_DIAMONDS = 2; // Rô
        public const int SUIT_HEARTS = 3; // Cơ
        public int number;
        public int suit;

        private Card(){
            this.number = -1; // 1 <= number <= 13
            this.suit = -1;   // 0 <= suit <= 3
        }
        static public Card Create(int value){
            if (value < 0 && value >= CardSet.MAX_CARDS)
                //throw new Exception("The cards dont have value " + value);
                return null;

            Card card = new Card();
            card.number = (value / 4 + 2) % 13 + 1;
            card.suit = value % 4;
            return card;
        }
        public int ToInt(){
            if (this.number < 1 || this.number > 13
                || this.suit < 0 || this.suit > 3)
                throw new Exception("Dont have any cards like {" + this.number + ", " + this.suit + "}");

            int t = this.number;
            if (this.number == 1 || this.number == 2)
                t = this.number + 13;

            int value = (t - 3) * 4 + this.suit;
            return value;
        }

        public bool IsLarger(Card card){
            return this.ToInt() > card.ToInt();
        }

        public override string ToString(){
            if (this.number == -1)
                return null;
            
            string c = "";
            switch(this.suit){
                case Card.SUIT_SPADES : c = "bich"; break;
                case Card.SUIT_CLUBS : c = "chuong"; break;
                case Card.SUIT_DIAMONDS: c = "ro"; break;
                case Card.SUIT_HEARTS: c = "co"; break;
                default : return null;
            }
            return "" + number + c;
        }
    }
}