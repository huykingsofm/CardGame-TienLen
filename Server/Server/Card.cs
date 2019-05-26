using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server
{
    public class Card : Object
    {
        public const int SUIT_SPADES = 1; // Bích
        public const int SUIT_CLUBS = 2; // Chuồn
        public const int SUIT_DIAMONDS = 3; // Rô
        public const int SUIT_HEARTS = 4; // Cơ
        public int number;
        public int suit;

        private Card()
        {
            this.number = -1; // 1 <= number <= 13
            this.suit = -1;   // 0 <= suit <= 3
        }
        static public Card Create(int num, int suit)
        {
            Card card = new Card();
            card.number = num;
            card.suit = suit;
            if (card.ToInt() < 0 || card.ToInt() >= 52)
                return null;
            return card;
        }

        static public Card Create(int value)
        {
            if (value < 0 && value >= CardSet.MAX_CARDS)
                //throw new Exception("The cards dont have value " + value);
                return null;

            Card card = new Card();
            card.number = (value / 4 + 2) % 13 + 1;
            card.suit = value % 4 + 1;
            return card;
        }
        public int ToInt()
        {
            if (this.number < 1 || this.number > 13
                || this.suit < 1 || this.suit > 4)
                throw new Exception("Dont have any cards like {" + this.number + ", " + this.suit + "}");

            int t = this.number;
            if (this.number == 1 || this.number == 2)
                t = this.number + 13;

            int value = (t - 3) * 4 + (this.suit - 1);
            return value;
        }

        public bool IsLarger(Card card)
        {
            return this.ToInt() > card.ToInt();
        }

        public override string ToString()
        {
            if (this.number == -1)
                return null;

            return "" + this.number + "_" + this.suit;
        }
    }
}