using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client {
    public class Buttons {
        public Button[] BtnArray { get; set; }
        public Buttons() { }
        public Buttons(Button btn1, Button btn2) {
            this.BtnArray = new Button[2];
            this.BtnArray[0] = btn1;
            this.BtnArray[1] = btn2;
        }
        public Buttons(Button[] btns) {
            this.BtnArray = btns;
        }
    }

    public class Player {
        private bool _hasTurn;
        private TcpClientModel _client;
        private Buttons _btns;
        private String[] _cards;
        private int _remainTime;

        public Player() { }
        public Player(TcpClientModel client) {
            this._hasTurn = false;
            this._client = client;
            this._cards = null;
            this._btns = null;
            this._remainTime = 30;
        }

        public Player(TcpClientModel client, Button btnPlay, Button btnSkip) {
            this._hasTurn = false;
            this._client = client;
            this._cards = null;
            this._btns = new Buttons(btnPlay, btnSkip);
            this._remainTime = 30;
        }

        public void Ready() {
            try {
                String req = RequestFormat.READY_GAME();

                this._client.SendRequest(req);
            } catch(Exception ex) {
                //do something
            }
        }

        public void StartGame() {
            try {
                String req = RequestFormat.START_GAME();

                this._client.SendRequest(req);
            } catch(Exception ex) {
                //do something
            }
        }

        public void ExitGame() {
            try {
                String req = RequestFormat.EXIT_ROOM();

                this._client.SendRequest(req);
            } catch(Exception ex) {
                //do something
            }
        }

        public void Play(String cards) {
            try {
                String req = RequestFormat.PLAY(cards);

                this._client.SendRequest(req);
            } catch(Exception ex) {
                //do something
            }
        }

        public void Play(String[] cards) {
            try {
                String req = RequestFormat.PLAY(cards);

                this._client.SendRequest(req);
            } catch (Exception ex) {
                //do something
            }
        }

        public void Skip() {
            try {
                String req = RequestFormat.SKIP();

                this._client.SendRequest(req);
            } catch(Exception ex) {
                //do something
            }
        }
    }
}
