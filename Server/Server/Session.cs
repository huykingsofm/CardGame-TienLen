using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Server{
    abstract public class Session{
        /*
        # Mục đích : Đại diện cho một phiên hoạt động của các thực thể
        # ..Các thực thể có session có thể giao tiếp với nhau
        # Hoạt động : + Một session khi khởi tạo, nó có thể bắt đầu nhận và gửi thông tin.
        #             + Session gọi phương thức Receive() để nhận một thông điệp được gửi đến bản thân.
        #             + Session gọi phương thức WaitReceive() để có thể chờ đến khi
        #               .. có dữ liệu được gửi về và lấy dữ liệu đó.
        #             + Session gọi phương thức Send(Session, Message) để gửi thông điệp với cú pháp 
        #               .. mặc định.
        #             + Session gọi phương thức SendCustom(Session, Message) để gửi thông điệp tùy chọn.
        #             + Session có một phương thức trừu tượng là Solve(Message), các dẫn xuất của nó cần
        #               .. định nghĩa phương thức này trong việc xử lý các thông điệp đã nhận.
        #               .. Sau đó, gọi phương thức Start() để bắt đầu xử lý các công việc ở một luồng
        #               .. khác, gọi Stop() để dùng luồng xử lý lại. 
        #             + Session gọi phương thức Destroy() để hủy bỏ tất cả các thông tin của bản thân,
        #               .. bao gồm các thông điệp được nhận, tuy nhiên các thông điệp đã gửi đi thì vẫn còn.
        #            
        # Cú pháp mặc định của thông điệp là "SessionName-ID=Message" 
        # .. với SessionName là tên Session, ID của là định danh của Session gửi 
        */
        public const int MAX_OBJ = 16;
        static Queue<Message>[] Request_Queue = null;
        static private int AvailableSlot;
        public int id {get; protected set;}
        private bool stop;
        protected bool stopforce;
        private Thread thread;
        public abstract string Name { get; }
        static Session(){
            Session.Request_Queue = new Queue<Message>[MAX_OBJ];
            Session.AvailableSlot = 0;
        }
        public Session(int limit = Session.MAX_OBJ){
            lock(Session.Request_Queue){
                // Tìm kiếm vị trí mới cho session
                int time = 0;
                while(Session.Request_Queue[Session.AvailableSlot] != null
                    && time < limit){
                    Session.AvailableSlot = (Session.AvailableSlot + 1) % Session.MAX_OBJ;
                    time += 1;
                }

                if (time >= limit)
                    throw new Exception("Server is full");

                // Thiết lập giá trị mới cho session
                this.thread = null;
                this.id = Session.AvailableSlot;
                this.stop = false;
                Session.Request_Queue[this.id] = new Queue<Message>();
            }
        }
        public Message Receive(){
            if (Session.Request_Queue[this.id].Count == 0)
                return null;
            return Session.Request_Queue[this.id].Dequeue();
        }
        public Message WaitReceive(int timeout = Int32.MaxValue, int interval_time = 10){
            int passed_time = 0;
            while (passed_time < timeout){
                Message message = this.Receive();
                Thread.Sleep(interval_time);
                
                if (message != null) // Nếu thông điệp tồn tại
                    return message;
                
                passed_time += interval_time;
            }

            // Nếu đã quá thời gian nhưng không nhân được thông điệp
            // throw new Exception("Timeout in receiving");
            return null;
        }
        public bool Send(Session session, string message){
            
            string default_message = String.Format(this.DefaultFormat(), 
                                                    this.id, message);
            return this.SendCustom(session, default_message);
        }
        public bool Send(int id, string message){
            string default_message = String.Format(this.DefaultFormat(), 
                                                    this.id, message);
            return SendCustom(id, default_message);
        }
        public bool SendCustom(Session session, string message){
            int id = session.id;
            return this.SendCustom(id, message);
        }
        public bool SendCustom(int id, string message){
            if (message == "")
                return false;
            
            Message m = Message.Create(message);
            lock(Session.Request_Queue[id]){
                try{
                    Session.Request_Queue[id].Enqueue(m);
                }
                catch{
                    throw new Exception("Session " + id + " do not exist");
                }
                return true;
            }
        }
        public abstract void Solve(object message);
        protected string DefaultFormat(){
            return "{0}-{1}={2}".Format(this.Name, "{0}", "{1}");
        }
        private void SolvingThread(){
            this.stop = false;
            this.stopforce = false;
            
            while(this.stopforce == false){
                try{
                    Message message = this.Receive();
                    Thread.Sleep(30);
                    
                    if (message == null){
                        if (this.stop == true)
                            break;
                        continue;
                    }
                    
                    Console.WriteLine("From {0} {1} '{2}'".Format(this.Name, this.id, message));
                    new Thread(this.Solve).Start(message);
                }
                catch(Exception e){
                    Console.WriteLine(e);
                }
            }
        }
        public virtual void Start(){
            if (this.thread != null){
                throw new Exception("Another solving thread is running");
            }
            Console.WriteLine("Start session {0} {1}".Format(this.Name, this.id));

            this.stop = false;
            this.thread = new Thread(this.SolvingThread);
            this.thread.Start();
        }
        public virtual void Stop(string mode = "normal"){
            if (this.thread == null){
                throw new Exception("Must be call Start() method before");
            }
            
            this.stop = true;
            if (mode == "force")
                this.stopforce = true;

            Thread.Sleep(1000);
            while (this.thread.IsAlive == true){
                Console.WriteLine("Wait for stopping {0} {1}".Format(this.Name, this.id));
                Thread.Sleep(1000);
            }

            this.thread = null;
            Console.WriteLine("End session {0} {1}".Format(this.Name, this.id));
        }
        public virtual void Destroy(string mode = "normal"){
            if(this.id == -1)
                return;
            
            if (this.thread != null)
                this.Stop(mode);
            Session.Request_Queue[this.id] = null;
            this.id = -1;
        }
    }
}