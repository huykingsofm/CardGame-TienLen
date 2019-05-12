using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class Lobby : Object{
        public const int MAX_ROOM = 10;
        List<ClientSession> clients;   
        RoomSession[] rooms;
        private OutdoorSession outdoor;
        private Lobby(LobbySession lobby, OutdoorSession outdoor){
            this.clients = new List<ClientSession>();
            this.rooms = new RoomSession[MAX_ROOM];
            this.outdoor = outdoor;
            for (int i = 0; i < this.rooms.Count(); i++){
                this.rooms[i] = new RoomSession(lobby);
                this.rooms[i].Start();
            }
        }
        public static Lobby Create(LobbySession lobby, OutdoorSession outdoor){
            if (lobby == null || outdoor == null)
                return null;
            return new Lobby(lobby, outdoor);
        }
        public bool Add(ClientSession client){
            if (client.IsAuthenticated() == false){
                return false;
            }

            lock(this.clients){
                this.clients.Add(client);    
            }
            
            return true;
        }
        public bool Join(ClientSession client, RoomSession room){
            int IndexClient = this.FindClient(client);
            int IndexRoom = this.FindRoom(room);

            if (IndexClient == -1 || IndexRoom == -1)
                return false;
                
                
            lock(this.clients){
                if (this.clients.Remove(this.clients[IndexClient]) == false)
                    throw new Exception("The user has not existed in lobby");
            }
            
            if (this.clients[IndexClient].Join(this.rooms[IndexRoom]) == false)
                throw new Exception("Error in server, fix bugs now");
    
            return true;
        }
        public void Pop(ClientSession client){
            lock(this.clients){
                if (this.clients.Remove(client) == false)
                    throw new Exception("Client do not exist in lobby");
            }        

            if (client.Join(this.outdoor) == false)
                throw new Exception("Error in server, need to fix bugs");    
        }
        public int FindRoom(int id){
            lock(this.rooms){
                for (int i = 0; i < this.rooms.Count(); i++)
                    if (this.rooms[i].id == id)
                        return i;
            }
            return -1;
        }
        public int FindRoom(RoomSession room){
            int id = room.id;
            return this.FindRoom(id);
        }
        public int FindClient(int id){
            lock(this.clients){
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i].id == id)
                        return i;
            }

            return -1;
        }
        public int FindClient(ClientSession client){
            int id = client.id;
            return this.FindClient(id);
        }

        public RoomSession GetRoom(int id){
            int index = this.FindRoom(id);
            if (index == -1)
                return null;
            
            lock(this.rooms)
                return this.rooms[index];
        }

        public ClientSession GetClient(int id){
            int index = this.FindClient(id);
            if (index == -1)
                return null;
            
            lock(this.clients)
                return this.clients[index];
        }

        public override string ToString(){
            string str = "";
            for (int i = 0; i < Lobby.MAX_ROOM; i++){
                str += this.rooms[i].ToString();
                if (i < Lobby.MAX_ROOM - 1)
                    str += ",";
            }
            return str;
        } 
        
        public int GetIdOutdoor(){
            return this.outdoor.id;
        }

        public void Destroy(LobbySession lobby){
            foreach(RoomSession room in this.rooms)
                room.Destroy();

            foreach(ClientSession client in this.clients.ToArray()){
                this.Pop(client);
                lobby.Send(this.outdoor, "ClientLeave:{0}".Format(client.id));
            }
        }

        public void UpdateForClients(LobbySession lobby){
            foreach(ClientSession client in this.clients)
                lobby.Send(client, "LobbyInfo:{0},{1}".Format(Lobby.MAX_ROOM, this.ToString()));
        }
    }
}