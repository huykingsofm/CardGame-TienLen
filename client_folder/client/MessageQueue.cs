using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace client {
    public static class MessageQueue {
        private static Queue<Message> _queue;
        private static TcpClientModel _tcp;
        private static bool isStop;

        public static void SetUp(TcpClientModel tcp) {
            _tcp = tcp;
            _queue = new Queue<Message>();
        }

        private static void ReceiveFromServer() {
            isStop = false;
            String lastMessage = null;
            while(!isStop) {
                String allMessage = _tcp.ReceiveResponse();

                if(allMessage == null) {
                    _tcp.DisConnectToServer();
                    break;
                }

                if(lastMessage != null) {
                    allMessage = lastMessage + allMessage;
                }

                int flag = allMessage.Last() != '|' ? 1 : 0;

                String[] messages = allMessage.Split('|');

                lastMessage = flag == 1 ? messages.Last() : null;

                for(int i = 0; i < messages.Count() - 1; i++) {
                    _queue.Enqueue(Message.Create(messages[i]));
                }
            }
        }

        public static void Start(TcpClientModel tcp) {
            MessageQueue.SetUp(tcp);

            Thread th = new Thread(ReceiveFromServer);
            th.Start();
        }

        public static Message GetMessage() {
            try {
                if (_queue.Count() == 0) {
                    return null;
                }

                return _queue.Dequeue();
            } catch(Exception ex) {
                Console.WriteLine("GetMessage error:" + ex.StackTrace);
                return null;
            }
        }
    }
}
