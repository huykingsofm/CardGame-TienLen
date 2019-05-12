using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Move : Object{
        /* 
        # Mục đích : Xây dựng một số phương thức cần thiết cho kiểm tra các
        #            .. nước bài, sự hợp lệ, ...
        */
        public static readonly int NONE = -1;
        public static readonly int SINGLE = 0;
        public static readonly int MULTI = 1;
        public static readonly int DOUBLE = 2;
        public static readonly int TRIPLE = 3;
        public static readonly int QUARTER = 4;
        public static readonly int MULTI_DOUBLE = 5;

        protected CardSet moveset;
        public int status{get; protected set;}
        public int[] values{get; protected set;}
        

        // - - - - - - CONSTRUCTOR - - - - - - 
        public Move(CardSet onhandset, CardSet moveset){
            this.moveset = moveset;

            if (this.IsMoveOnHand(onhandset) == false){
                this.status = -1;
                this.values = null;
                return;
            }

            this.SetupStatus();
            this.SetupValues();
        }

        public static Move Create(CardSet onhandset, CardSet movesetset){
            Move move = new Move(onhandset, movesetset);
            if (move.status == -1)
                return null;
            return move;
        }

        // - - - - - - METHOD - - - - - - - 
        private bool IsMoveOnHand(CardSet onhandset){
            
            // Kiểm tra các quân bài trong move có tồn tại trong tập 
            // ..bài trên tay không
            for (int i = 0; i < CardSet.MAX_CARDS; i++)
                if (this.moveset.cards[i] == true && onhandset.cards[i] == false)
                    return false;

            return true;
        }
        private void SetupStatus(){
            List<Card> list = this.moveset.ToList();

            // Kiểm tra nó có phải là lá đơn không
            if (list.Count() == 1)
                this.status = Server.Move.SINGLE;

            // Kiểm tra nó có phải là đôi không
            if (list.Count() == 2){
                if (list[0].number == list[1].number)
                    this.status = Server.Move.DOUBLE;
            }
            
            // Kiểm tra nó có phải là bộ ba không
            if (list.Count() == 3){
                if (list[0].number == list[1].number && list[1].number == list[2].number)
                    this.status = Server.Move.TRIPLE;
            }

            // Kiểm tra nó có phải là bộ tứ (tứ quý) không
            if (list.Count() == 4){
                if (list[0].number == list[1].number 
                && list[1].number == list[2].number
                && list[2].number == list[3].number)
                
                this.status = Server.Move.QUARTER;
            }

            // Kiểm tra nó có phải là sảnh không
            if (list.Count() >= 3){
                bool IsMulti = true;

                for (int i = 1; i < list.Count(); i++){
                    if (list[i].number != list[i - 1].number + 1)
                        IsMulti = false;
                }

                if (IsMulti)
                    this.status = Server.Move.MULTI;
            }

            // Kiểm tra nó có phải là sảnh đôi (đôi thông) không
            if (list.Count() >= 3 && list.Count() % 2 == 0){
                bool IsMultiDouble = true;
                
                for (int i = 1; i < list.Count(); i++){
                    if (i % 2 == 1){
                        if (list[i].number != list[i - 1].number)
                            IsMultiDouble = false;
                    }
                    else{
                        if (list[i].number != list[i - 1].number + 1)
                            IsMultiDouble = false;
                    }
                }

                if (IsMultiDouble)
                    this.status = Server.Move.MULTI_DOUBLE;
            }

            this.status = -1;
        }
        private void SetupValues(){
            List<Card> list = this.moveset.ToList();

            if (this.status == Server.Move.SINGLE
              ||this.status == Server.Move.DOUBLE
              ||this.status == Server.Move.TRIPLE
              || this.status == Server.Move.QUARTER){

                this.values = new int[1];
                this.values[0] = list.Last().ToInt(); 
                // giá trị của nước đi này là giá trị của lá cuối cùng 
            }
            else if (this.status == Server.Move.MULTI || this.status == Server.Move.MULTI_DOUBLE){ 
                this.values = new int[2];
                this.values[0] = list.Last().ToInt();
                this.values[1] = list.Count();
                // giá trị của nước đi này là giá trị của lá cuối cùng + số lượng lá
            }
        }
        public int IsValid(Move prev){
            /* 
            # Mục đích : Kiểm tra nước đi hiện tại có phù hợp với nước đi trước đó không
            # Trả về : + 0 --> không thể thực hiện
            #          + 1 --> thực hiện bình thường
            #          + 2 --> thực hiện bình thường, nhưng là nước đi đặc biệt : bắt lá 2
            #          + 3 --> thực hiện bình thường, nhưng là nước đi đặc biệt : bắt đôi thông
            # Các mục kiểm tra gồm : 
            #   +(1) prev == null --> Nước đi hiện tại (sau gọi là nước đi) luôn khả thi.
            #   +(2) prev == lá đơn --> Nước đi phải là lá đơn và giá trị lớn hơn
            #                       .. nước đi trước đó. Trường hợp đặc biệt, nếu nước
            #                       .. đi trước là lá 2, thì tứ quý và đôi thông sẽ 
            #                       .. được cho phép.
            #   +(3) prev = bộ đôi --> nước đi phải là bộ đôi và có giá trị cao hơn.
            #   +(4) prev = bộ ba  --> nước đi phải là bộ ba và có giá trị cao hơn.
            #   +(5) prev = bộ tứ (tứ quý) --> nước đi phải là bộ tứ và có giá trị cao hơn.
            #   +(6) prev = sảnh --> nươc đi phải là sảnh với số lá như nhau và có giá 
            #                     .. trị cao hơn.
            #   +(7) prev = đôi thông --> nước đi phải là đôi thông có số lá như nhau 
            #                          .. và có giá trị cao hơn. Trường hợp đặc biệt,
            #                          .. nếu là ba đôi thông, tứ quý được cho phép.
            */

            //(1)
            if (prev == null)
                return 1;

            Card prevValue = Card.Create(prev.values[0]);
            Card thisValue = Card.Create(this.values[0]); 

            //(2)
            if (prev.status == Server.Move.SINGLE){
                if (prevValue.number == 2){ // Xử lý trường hợp lá 2 trước
                    if (this.status == Server.Move.QUARTER || this.status == Server.Move.MULTI_DOUBLE)
                        return 2;
                }

                // Xử lý tất cả trường hợp còn lại
                if (this.status == Server.Move.SINGLE){
                    if (this.values[0] > prev.values[0])
                        return 1;
                }

                return 0;
            }

            //(3)
            if (prev.status == Server.Move.DOUBLE){
                if (this.status == Server.Move.DOUBLE && this.values[0] > prev.values[0])
                    return 1;

                return 0;
            }

            //(4)
            if (prev.status == Server.Move.TRIPLE){
                if (this.status == Server.Move.TRIPLE && this.values[0] > prev.values[0])
                    return 1;

                return 0;
            }

            
            //(5)
            if (prev.status == Server.Move.QUARTER){
                if (this.status == Server.Move.QUARTER && this.values[0] > prev.values[0])
                    return 1;

                return 0;
            }

            
            //(6)
            if (prev.status == Server.Move.MULTI){
                if (this.status == Server.Move.MULTI 
                && this.values[0] > prev.values[0]
                && this.values[1] == prev.values[1]) // Cùng số lá
                    return 1;

                return 0;
            }
            
            //(7)
            if (prev.status == Server.Move.MULTI_DOUBLE){
                // Xử lý trường hợp ba đôi thông (6 lá)
                if (prev.values[1] == 6){
                    if (this.status == Server.Move.QUARTER)
                        return 3;
                }

                // Các trường hợp còn lại
                if (this.status == Server.Move.MULTI_DOUBLE 
                    && this.values[0] > prev.values[0]
                    && this.values[1] == prev.values[1])
                    return 1;
 
                return 0;
            }

            return 0;
        }
        public CardSet GetMoveSet(){
            return this.moveset.Clone();
        }
    }
}