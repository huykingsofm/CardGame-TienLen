using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    class Room : Object{
        public int count{get; private set;}
        public int host{get; private set;}
        public int BetMoney{get; private set;}
        private ClientSession[] clients;
        private GameSession game;
        private LobbySession lobby;
        private bool StopThreadCheckGame;
        public int index{get; private set;}
        private int[] ClientStatus; /*  
                                    #    0 - không trong phòng
                                    #    1 - có trong phòng nhưng chưa sẵn sàng
                                    #    2 - có trong phòng và đã sẵn sàng
                                    #    3 - có trong phòng và đang chơi game
                                    */
        private static int NOT_IN_ROOM = 0;
        private static int NOT_READY = 1;
        private static int READY = 2;
        private static int PLAYING = 3;
        private int RoomStatus;     /*
                                    #    0 - Phòng không chơi
                                    #    1 - Phòng đang chơi
                                    */
        public Room(LobbySession lobby, int index){
            this.count = 0;
            this.host = -1;
            this.BetMoney = 0;
            this.game = null;
            this.index = index;
            this.ClientStatus = new int[]{0, 0, 0, 0}; // NOT_IN_ROOM
            this.RoomStatus = 0;
            this.clients = new ClientSession[]{null, null, null, null};
            this.lobby = lobby;
        }

        public int FindIndex(ClientSession client){
            for (int i = 0; i < this.clients.Count(); i++)
                if (this.clients[i] == client && 
                    this.ClientStatus[i] != Room.PLAYING)
                    return i;
            return -1;
        }
        public int FindIndexById(int id){
            for (int i = 0; i < this.clients.Count(); i++)
                if (this.clients[i].id == id && 
                    this.ClientStatus[i] != Room.PLAYING)
                    return i;
            return -1;
        }

        public bool Add(ClientSession client){
            if (this.FindIndex(client) != -1)
                throw new Exception("User has been in this room");

            for (int i = 0; i < this.clients.Count(); i++)
                if (this.ClientStatus[i] == Room.NOT_IN_ROOM){
                    this.clients[i] = client;
                    this.ClientStatus[i] = Room.NOT_READY;
                    
                    if (this.host == -1)
                        this.host = i;
                    this.count++;
                    return true;
                }
            return false;
        }

        protected bool Pop(int index){
            if (this.clients[index] != null){
                ClientSession client = this.clients[index];
                this.clients[index] = null;
                this.ClientStatus[index] = NOT_IN_ROOM;

                if (index == this.host){
                    this.host = -1;
                    
                    // Tìm host mới
                    lock(this.clients){
                        for (int i = 0; i < this.clients.Count(); i++)
                            if (this.clients[i] != null){
                                this.host = i;
                                break;
                            }
                    }
                }

                if(client.Join(this.lobby) == false)
                    throw new Exception("Error in server, fix bugs now");
                this.count -= 1;
                return true;
            }
            throw new Exception("The client[{0}] is not existed in room".Format(index));
        }

        public bool Pop(ClientSession client){
            int index = this.FindIndex(client);
            if (index != -1)
                return this.Pop(index);
            return false;
        }
        public bool PopById(int id){
            int index = this.FindIndexById(id);
            if (index != -1)
                return this.Pop(index);
            return false;
        }

        protected void ReadyByIndex(int index){
            this.ClientStatus[index] = Room.READY;
        }
        public bool Ready(ClientSession client){
            int index = this.FindIndex(client);
            try{
                this.ReadyByIndex(index);
            }
            catch{
                return false;
            }
            return true;
        }
        public bool ReadyById(int id){
            int index = this.FindIndexById(id);
            try{
                this.ReadyByIndex(index);
            }
            catch{
                return false;
            }
            return true;
        }
        protected void UnReadyByIndex(int index){
            this.ClientStatus[index] = Room.NOT_READY;
        }
        public bool UnReady(ClientSession client){
            int index = this.FindIndex(client);
            try{
                this.UnReadyByIndex(index);
            }
            catch{
                return false;
            }
            return true;
        }
        public bool UnReadyById(int id){
            int index = this.FindIndexById(id);
            try{
                this.ReadyByIndex(index);
            }
            catch{
                return false;
            }
            return true;
        }
    
        private void CheckEndGame(){
            this.StopThreadCheckGame = false;
            while(this.game.IsEnd() == false || this.StopThreadCheckGame == false);
            // do something
            this.game.WriteLog();
            this.game = null;
        }
        public bool StartGame(){
            if (this.count < 2)
                return false;

            lock(this.clients){
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null && this.ClientStatus[i] != Room.READY)
                        return false;

                this.game = GameSession.Create(this.clients, this.BetMoney, this.host);
                
                if (game == null)
                    return false;

                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i] != null)
                        this.ClientStatus[i] = Room.PLAYING;
            }

            this.game.Start();
            
            Thread t = new Thread(this.CheckEndGame);
            t.Start();

            return true;
        }

        public void StopGame(){
            if (this.game != null){
                this.game.Destroy();
                this.StopThreadCheckGame = true;
            }
        }

        public void Destroy(){
            this.StopGame();
            foreach (ClientSession client in this.clients)
                if (client != null)
                    this.Pop(client);
        }

        public override string ToString(){
            return "{0},{1}".Format(this.count, this.BetMoney);
        } 

        public int GetLobbyId(){
            return this.lobby.id;
        }

    }
}