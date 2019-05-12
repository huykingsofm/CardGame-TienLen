using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    public class OutdoorSession : Session{
        TcpServer server;
        List<ClientSession> clients;
        LobbySession lobby;
        Thread OutdoorThread;
        bool OutdoorStop;
        public override string Name => "Outdoor";

        public OutdoorSession() : base(){
            this.server = new TcpServer("192.168.137.132", 0409);
            this.server.Listen();
            this.lobby = new LobbySession(this);
            this.lobby.Start();
            this.clients = new List<ClientSession>();
            this.OutdoorThread = null;
            this.OutdoorStop = false;
        }

        public void WaitForNewClient(){
            this.OutdoorStop = false;
            while (this.OutdoorStop == false){
                SimpleSocket s = this.server.AcceptSimpleSocket();
                
                if (s == null)
                    continue;

                ClientSession client;
                try{
                    client = new ClientSession(s, this);
                }
                catch(Exception e){
                    s.Send("Failure:{0}".Format(e.Message));
                    s.Close();   
                    continue;
                }

                this.clients.Add(client);
                client.Start();
            }
        }
        public override void Solve(object obj){
            Message message = (Message) obj;
            int index = this.Find(message.id);

            if (this.lobby.id == message.id)
                index = Int32.MaxValue;

            if (index == -1){
                Console.WriteLine("From {0} {1} : Cannot identify message".Format(this.Name, this.id));
                return;
            }

            switch(message.name){
                case "ClientLeave":
                    /* 
                    # Nhận thông tin hủy đi một client từ lobby, gọi hàm client.Destroy
                    */
                    if (index != Int32.MaxValue)
                        return;
                        
                    if (message.args == null || message.args.Count() != 1){
                        Console.WriteLine("From Outdoor : Cannot solve {0}".Format(message));
                        return;
                    }
                    int id;
                    try{
                        id = Int32.Parse(message.args[0]);
                    }
                    catch{
                        Console.WriteLine("From Outdoor : Cannot solve {0}".Format(message));
                        return;
                    }

                    index = this.Find(id);
                    if (index == -1){
                        Console.WriteLine("Client do not exist");
                        return;
                    }

                    this.Pop(this.clients[index]);
                    break;
                case "JoinLobby":
                    /*
                    # Nhận thông tin vào một lobby của một client
                    # Điều kiện là client phải được xác thực
                    */
                    if (index == Int32.MaxValue)
                        return;

                    if (this.clients[index].IsAuthenticated() == false){
                        this.Send(this.clients[index], "Failure:Please log in before");
                        return;
                    }
                    
                    bool bSuccess = this.clients[index].Join(this.lobby);
                    
                    if (bSuccess)
                        this.clients.Remove(this.clients[index]);
                    else
                        this.Send(this.clients[index], "Failure:Cannot join lobby");
                    
                    break;
                case "Disconnect":
                    /*
                    # Nhận thông tin tự đăng xuất của client
                    */
                    if (message.which != "Client")
                        return;

                    this.Pop(message.id);
                    break;
                default:
                    Console.WriteLine("From {0} {1} : Cannot identify message".Format(this.Name, this.id));
                    break;
            }
        }

        public override void Start(){
            if (this.OutdoorThread != null)
                throw new Exception("You has called Start() method before");
            
            this.OutdoorThread = new Thread(this.WaitForNewClient);
            this.OutdoorThread.Start();
            base.Start();
        }

        public override void Stop(string mode = "normal"){
            if (this.OutdoorThread == null)
                throw new Exception("It must call Start() method before call Stop()");

            this.OutdoorStop = true;
            Thread.Sleep(100);
            while(this.OutdoorThread.IsAlive){
                Console.WriteLine("Wait for stopping {0} {1}".Format(this.Name, this.id));
                Thread.Sleep(100);
            }

            this.OutdoorThread = null;
            base.Stop(mode);
        }
        public override void Destroy(string mode = "normal"){
            this.lobby.Destroy();
            base.Destroy(mode);
        }
        public int Find(int id){
            lock(this.clients){
                for (int i = 0; i < this.clients.Count(); i++)
                    if (this.clients[i].id == id)
                        return i;
            }
            return -1;
        }
        public bool Add(ClientSession client){
            if(this.Find(client.id) != -1)
                return false;

            lock(this.clients){
                this.clients.Add(client);
            }

            return true;
        }
        public bool Pop(int id){
            int index = this.Find(id);
            this.clients[index].Destroy();
            
            lock(this.clients){
                this.clients.Remove(this.clients[index]);
            }
            
            return false;
        }

        public bool Pop(ClientSession client){
            bool bSuccess = this.clients.Remove(client);
            if (bSuccess)
                client.Destroy();
            return bSuccess;
        }
    }
}