using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Json;

namespace Server{
    class Gate : Thing{
        /*
         * Mục đích : Một lớp liên tục tiếp nhận các client và gửi cho outdoor.
         * Thuộc tính : 
         *      + outdoorsession : outdoorsession mà gate cần gửi client đến.
         *      + server         : tcpserver giúp việc tiếp nhận client.
         *      + thread         : giúp kiểm soát luồng tiếp nhận.
         *      + stop           : hỗ trợ duy trì hoặc kết thúc luồng tiếp nhận.
         * Khởi tạo : 
         *      + Gate(OutdoorSession)   : không thể truy cập trực tiếp.
         *      + Create(OutdoorSession) : tạo ra một gate, trả về null nếu không thể tạo.
         * Phương thức : 
         *      + Start()            : Bắt đầu luồng tiếp nhận client.
         *      + WaitForNewClient() : Liên tục tiếp nhận client và đưa vào outdoorsession.  
         *      + Stop()             : Dừng luồng tiếp nhận client.
         */
        public override string Name => "Gate";
        private OutdoorSession outdoorsession;
        private TcpServer server;
        private Thread thread;
        private bool stop;
        private Gate(OutdoorSession outdoorsession){
            if (outdoorsession == null)
                throw new Exception("Outdoor must be not null instance");

            this.outdoorsession = outdoorsession;
        }

        public static Gate Create(OutdoorSession outdoor){
            Gate gate = null;
            try{
                gate = new Gate(outdoor);
            }
            catch(Exception e){
                Console.WriteLine(e.Message);
                return null;
            }
            return gate;
        }
        public void WaitForNewClient(){
            // Thiết lập giá trị ban đầu cho stop, giúp duy trì vòng lặp tiếp nhận.
            this.stop = false;

            // Nếu stop được chỉ định là true(chỉ từ một luồng khác), thì dừng việc
            // .. tiếp nhận.
            while (this.stop == false){
                // Nhận SimpleSocket từ server
                SimpleSocket s = this.server.AcceptSimpleSocket();
    
                // Trường hợp vượt quá timeout
                if (s == null)
                    continue;

                // Khởi tạo clientsession
                Client client;
                ClientSession clientsession;
                try{
                    client = Client.Create(s);
                    clientsession = ClientSession.Create(client, this.outdoorsession);
                }
                catch(Exception e){
                    s.Send("Failure:{0}".Format(e.Message));
                    s.Close();   
                    continue;
                }

                // Nếu không có lỗi, thêm client vào outdoor và start session đó.
                try{
                    clientsession.Start();
                    this.outdoorsession.Add(clientsession);
                }
                catch(Exception e){
                    this.WriteLine(e.Message);
                }
            }
        }
        public Thread Start(){
            // Kiểm tra đã khởi tạo luồng trước đó chưa?
            if (this.thread != null)
                throw new Exception("Server has started before, stop it before start again");

            // Tiếp nhận dữ liệu khởi tạo từ file server.ini
            // Nếu không có file hoặc bị lỗi, sẽ khởi tạo bằng socket mặc định 127.0.0.1:1999
            string IP = null;
            int Port = 0;
            try{
                using(var f = new JsonReader("server.ini")){
                    JsonValue initserver = f.Read();
                    IP = initserver["IP"];
                    Port = initserver["Port"];
                }
            }catch(Exception e){
                this.WriteLine(e.Message);
                this.WriteLine("Create server with default socket 127.0.0.1 : 1999");
                IP = "127.0.0.1";
                Port = 1999;
            }
            finally{
                this.WriteLine("Server start at ({0}:{1})", IP, Port);
            }

            // Khởi động server
            this.server = new TcpServer(IP, Port);
            this.server.Listen();

            // Bắt đầu luồng tiếp nhận client sau khi server được bật
            this.thread = new Thread(this.WaitForNewClient);
            this.thread.Start();

            return this.thread;
        }

        public void Stop(){
            // Kiểm tra server đã bật chưa?
            if (this.thread == null)
                throw new Exception("Please start before stop");
 
            this.stop = true;
            Thread.Sleep(500);
            while (this.thread.IsAlive == true){
                this.WriteLine("Wait for thread");
                Thread.Sleep(500);
            }

            this.thread = null;
            this.WriteLine("Close thread successfully");
        }
    }
}