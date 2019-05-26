using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Json;

namespace Server{
    public class OutdoorSession : Session{
        /*
         * Mục đích : Tạo một Session đại diện cho outdoor - khiến outdoor có thể tự phản
         *            .. ứng nhanh với một vài sự kiện xảy ra.
         * Thuộc tính : 
         *      + outdoor        : chứa outdoor mà nó đại diện.
         *      + clientsessions : mảng chứa các clientsession đại diện cho các client 
         *                         .. bên trong outdoor.
         *      + lobbysession   : chứa lobbysession đại diện cho lobby trong outdoor.
         * Khởi tạo :
         *      + OutdoorSession()  : Không thể truy cập trực tiếp.
         *      + static Create()   : Tạo một đối tượng outdoorsession, nếu không thể tạo,
         *                            .. trả về null.
         * Phương thức :
         *      + Solve(Message)        : Giải quyết một message khi được nhận từ các session khác.
         *      + Add(ClientSession)    : Thêm một ClientSession.
         *      + Remove(ClientSession) : Loại bỏ một ClientSession.
         *      + Và các phương thức được kế thừa từ lớp Session. Xem Session để biết thêm chi tiết.
         */
        private Outdoor outdoor;
        private ClientSession[] clientsessions;
        private LobbySession lobbysession;
        public override string Name => "OutdoorSession";

        private OutdoorSession() : base(){
            this.outdoor = Outdoor.Create();
            this.lobbysession = LobbySession.Create(this.outdoor.lobby, this);
            this.lobbysession.Start();

            this.clientsessions= new ClientSession[Outdoor.MAX_CLIENT];

            for (int i = 0; i < this.clientsessions.Count(); i++)
                this.clientsessions[i] = null;
        }
        public static OutdoorSession Create(){
            OutdoorSession outdoorsession;
            try{
                outdoorsession = new OutdoorSession();
            }
            catch(Exception e){
                Console.WriteLine(e);
                return null;
            }

            return outdoorsession;
        }
        public override void Solve(object obj){
            Message message = (Message) obj;

            if (
                this.clientsessions.FindById(message.id) == -1 
                && message.id != this.lobbysession.id
            ){
                this.WriteLine("Cannot identify message");
                return;
            }

            switch(message.name){
                case "JoinLobby":{
                    /*
                    # Nhận thông tin vào một lobby của một client
                    # Điều kiện là client phải được xác thực
                    */
                    int index = this.clientsessions.FindById(message.id);
                    if (index == -1){
                        this.WriteLine("JoinLobby must come from Client");
                        return;
                    }

                    if (this.outdoor.clients[index].IsLogin() == false){
                        this.Send(this.clientsessions[index], "Failure:JoinLobby,Please log in before");
                        return;
                    }

                    try{
                        this.lobbysession.Add(this.clientsessions[index]);
                        this.clientsessions[index].Join(this.lobbysession);
                        this.Remove(this.clientsessions[index]);
                    }
                    catch(Exception e){
                        this.WriteLine(e.Message);
                        this.Send(this.clientsessions[index], "Failure:JoinLobby,Cannot join lobby");
                        return;
                    }
                    break;
                }
                case "Disconnect":{
                    /*
                    # Nhận thông tin tự đăng xuất của client
                    */
                    if (this.clientsessions.FindById(message.id) == -1){
                        this.WriteLine("Disconnect must be come from Client");
                        return;
                    }

                    try{
                        int index = this.clientsessions.FindById(message.id);
                        this.clientsessions[index].Destroy();
                        this.Remove(this.clientsessions[index]);
                    }
                    catch (Exception e){
                        this.WriteLine(e.Message);
                        return;
                    }
                    break;
                }
                default:
                    this.WriteLine("Cannot identify message");
                    break;
            }
        }
        public void Add( ClientSession clientsession){
            if (clientsession.client.IsAlive() == false){
                clientsession.Destroy();
                return;
            }
            int index = this.outdoor.Add(clientsession.client);
            this.clientsessions[index] = clientsession;
        }
        public void Remove(ClientSession clientsession){
            int index = this.outdoor.Remove(clientsession.client);
            this.clientsessions[index] = null;
        }
        public override void Destroy(string mode = "normal"){
            this.lobbysession.Destroy(mode);
            
            foreach(var clientsession in this.clientsessions)
                if (clientsession != null)
                    clientsession.Destroy(mode);

            base.Destroy(mode);
        }
    }
}