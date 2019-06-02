using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client {
    public partial class frmRoom : Form {
        public frmRoom() {
            InitializeComponent();
        }

        public frmRoom(TcpClientModel tcpModel, frmLobby parent) {
            InitializeComponent();
            this._client = tcpModel;
            this._player = new Player(this._client, btnPlay, btnSkip);
            this._parent = parent;
            this.isStop = false;

        }

        static int CARD_WIDTH = 71;
        static int CARD_MIN_WIDTH = 64;
        static int CARD_HEIGHT = 100;
        static int CARD_MIN_HEIGHT = 64;
        static int Y_LOCATION = 500;
        static int X_BET_LOCATION = 790;

        private TcpClientModel _client;
        private Player _player;
        private frmLobby _parent;
        private bool isStop;
        private PictureBox[] _cards;
        private List<PictureBox> _playedCards = new List<PictureBox>();

        public Control[] MapIndexToControls(int index) {
            try {
                if (index < 0 || index > 3) return null;

                Control[] c = new Control[7];
                switch(index) {
                    case 0:
                        c[0] = lblPlayerName0;
                        c[1] = lblRemainCoin0;
                        c[2] = null;
                        c[3] = null;
                        c[4] = pbox0;
                        c[5] = null;
                        c[6] = null;
                        break;
                    case 1:
                        c[0] = lblPlayerName1;
                        c[1] = lblRemainCoin1;
                        c[2] = lblRemainCards1;
                        c[3] = pboxRemainCards1;
                        c[4] = pbox1;
                        c[5] = null;
                        c[6] = lblHost1;
                        break;
                    case 2:
                        c[0] = lblPlayerName2;
                        c[1] = lblRemainCoin2;
                        c[2] = lblRemainCards2;
                        c[3] = pboxRemainCards2;
                        c[4] = pbox2;
                        c[5] = null;
                        c[6] = lblhost2;
                        break;
                    case 3:
                        c[0] = lblPlayerName3;
                        c[1] = lblRemainCoin3;
                        c[2] = lblRemainCards3;
                        c[3] = pboxRemainCards3;
                        c[4] = pbox3;
                        c[5] = null;
                        c[6] = lblhost3;
                        break;
                }

                return c;
            } catch(Exception ex) {
                //do somrthing
                return null;
            }
        }

        private void DisablePlayerUI(Control[] controls) {
            try {
                foreach(Control c in controls) {
                    if(c != null) {
                        c.Enabled = false;
                    }
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void EnablePlayerUI(Control[] controls) {
            try {
                foreach (Control c in controls) {
                    if (c != null) {
                        c.Enabled = true;
                    }
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void ToggleStartButtonForHost(String[] args) {
            try {
                int numPlayer = 0;
                int flag = 0;
                for(int i = 4; i <= 10; i += 3) {
                    String status = args[i];
                    if (status == "0") numPlayer++;
                    if (status == "2") {
                        numPlayer++;
                        flag++;
                    }
                }

                if(numPlayer == flag && flag != 0) {
                    btnReady.Visible = false;
                    btnStartGame.Enabled = true;
                    btnStartGame.Visible = true;
                    btnStartGame.Text = "Start";
                    btnStartGame.BackColor = Color.Green;
                } else {
                    btnReady.Visible = false;
                    btnStartGame.Enabled = false;
                    btnStartGame.Visible = true;
                    btnStartGame.Text = "Waiting...";
                    btnStartGame.BackColor = Color.White;
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void ReadyFailed(String message) {
            try {
                MessageBox.Show(message, "Ready failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                btnStartGame.Visible = false;
                btnReady.Enabled = true;
                btnReady.Visible = true;
                btnReady.Text = "READY";

                this._player.isReady = false;
            } catch(Exception ex) {
                //do something
            }
        }

        private void UnReadyFailed(String message) {
            try {
                MessageBox.Show(message, "Unready failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                btnStartGame.Visible = false;
                btnReady.Enabled = true;
                btnReady.Visible = true;
                btnReady.Text = "UN-READY";

                this._player.isReady = true;
            } catch (Exception ex) {
                //do something
            }
        }

        private void StartFailed(String message) {
            try {
                MessageBox.Show(message, "Failed Start", MessageBoxButtons.OK, MessageBoxIcon.Error);

                btnReady.Visible = false;

                btnStartGame.Visible = true;
                btnStartGame.Enabled = true;
                btnStartGame.Text = "START";
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdatePlayerByStatus(Control[] c, String[] playerInfo, bool isThisPlayer = false, bool isHost = false) {
            try {
                int status = Convert.ToInt32(playerInfo[2]);

                if(!isThisPlayer) {
                    switch(status) {
                        case 0:
                            c[0].Text = "No body";
                            c[1].Text = "...";
                            c[2].Text = "...";
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/nobody.png");
                            //c[5] for remainTime 
                            c[6].Visible = isHost;

                            //UI
                            this.DisablePlayerUI(c);
                            break;
                        case 1:
                            c[0].Text = playerInfo[0] + " (UR)";
                            c[1].Text = playerInfo[1];
                            c[2].Text = "...";
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime
                            c[6].Visible = isHost;

                            //UI
                            this.EnablePlayerUI(c);
                            break;
                        case 2:
                            c[0].Text = playerInfo[0] + " (R)";
                            c[1].Text = playerInfo[1];
                            c[2].Text = "...";
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime
                            c[6].Visible = isHost;

                            //UI
                            this.DisablePlayerUI(c);
                            break;
                        case 3:
                            c[0].Text = playerInfo[0] + " (IG)";
                            c[1].Text = playerInfo[1];
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime
                            c[6].Visible = isHost;

                            //UI
                            this.DisablePlayerUI(c);
                            break;
                    }
                } else {
                    c[0].Text = playerInfo[0];
                    c[1].Text = playerInfo[1];

                    this._player.isHost = isHost;

                    //UI
                    this.EnablePlayerUI(c);
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateRoom(String[] args) {
            try {
                int length = 12;
                String[] roomInfo = new String[length];
                Array.Copy(args, 2, roomInfo, 0, length);

                int hostIndex = Convert.ToInt32(args[1]);
                if(hostIndex == 0) {
                    this.ToggleStartButtonForHost(args);
                } else {
                    this.ToggleReadyUI();
                }

                int index = 0;
                for(int i = 0; i <= length - 3; i += 3) {
                    String[] playerInfo = new String[3];
                    Array.Copy(args, i, playerInfo, 0, 3);
                    Control[] c = this.MapIndexToControls(index);

                    if(c != null) {
                        if(index == hostIndex) this.UpdatePlayerByStatus(c, playerInfo, true, true);
                        else this.UpdatePlayerByStatus(c, playerInfo, true, false);
                    }
                    index++;
                }
            } catch(Exception ex) {
                //do something
                Console.WriteLine("From room, an exception occurred");
            }
        }

        private void ToggleReadyUI() {
            try {
                if(this._player.isReady) {
                    btnReady.Enabled = true;
                    btnReady.Visible = true;
                    btnReady.Text = "UN_READY";

                    btnStartGame.Visible = false;
                } else {
                    btnReady.Enabled = true;
                    btnReady.Visible = true;
                    btnReady.Text = "READY";

                    btnStartGame.Visible = false;
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private String GetSelectedCards() {
            try {
                int length = this._cards.Length;
                String tmp = "";

                for(int i = 0; i < length; i++) {
                    PictureBox pb = this._cards[i];
                    if(Convert.ToInt32(pb.Tag) == 1) {
                        if(i == 0) {
                            tmp += pb.Name;
                        } else {
                            tmp += "," + pb.Name;
                        }                      
                    }
                }

                if(tmp.Length > 0) {
                    if(tmp[0] == ',') {
                        return tmp.Substring(1);
                    }
                    return tmp;
                }
                return null;
            } catch(Exception ex) {
                //do something
                return null;
            }
        }

        private void UpdateMyCardsUI(String[] cards) {
            try {
                int numCards = cards.Length;
                int begPosY = Y_LOCATION;
                int begPosX = X_BET_LOCATION + (numCards * (CARD_WIDTH - 10)) / 2;

                PictureBox[] c = new PictureBox[numCards];
                for(int i = numCards - 1; i >= 0; i--) {
                    int posX = i == numCards - 1 ? begPosX : c[i + 1].Location.X - CARD_WIDTH + 10;

                    c[i].Width = CARD_WIDTH;
                    c[i].Height = CARD_HEIGHT;
                    c[i].Name = cards[i];
                    c[i].Tag = 0;

                    Image img = Image.FromFile(@"cards/" + c[i].Name + ".png");
                    c[i].Image = img;

                    c[i].Location = new Point(posX, begPosY);
                }

                this._cards = c;

                this.Invoke((MethodInvoker)delegate {
                    this.Controls.AddRange(c);
                });
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateOpponentCards(String[] args, int n) {
            try {
                for(int i = 1; i <= 3; i++) {
                    Control[] c = this.MapIndexToControls(i);
                    c[2].Text = args[n + i];
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateCardsUI(String[] args) {
            try {
                int numCards = Convert.ToInt32(args[0]);
                String[] cards = new String[numCards];

                Array.Copy(args, 1, cards, 0, numCards);
                this._player.UpdateCards(cards);
                this.UpdateMyCardsUI(cards);

            } catch(Exception ex) {
                //do something
            }
        }

        private void HandleResponse(Object obj) {
            Message message = (Message)obj;
            String[] args = message.args;
            Console.WriteLine("From Room, handleResponse: " + message);

            switch (message.name) {
                case "RoomInfo":
                    this.UpdateRoom(args);
                    break;
                case "Failure":
                    if(args[0] == "Ready") {
                        this.ReadyFailed(args[1]);
                    }
                    if(args[0] == "UnReady") {
                        this.UnReadyFailed(args[1]);
                    }
                    if(args[0] == "Start") {
                        this.StartFailed(args[1]);
                    }
                    break;
            }
        }

        public void StartHandleReponses() {
            this.isStop = false;

            while (!isStop) {
                Message m = MessageQueue.GetMessage();
                if (m == null) continue;

                this.HandleResponse(m);
            }
        }

        private void frmRoom_Load(object sender, EventArgs e) {
            try {
                Thread th = new Thread(this.StartHandleReponses);
                th.Start();
            } catch(Exception ex) {
                //do something
            }
        }

        private void frmRoom_FormClosing(object sender, FormClosingEventArgs e) {
            try {

            } catch(Exception ex) {
                DialogResult dr = MessageBox.Show(
                    "If you exit game, you will lose your coin in this game. Still exit ?", 
                    "Exit Room", 
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                    );

                if(dr == DialogResult.No) {
                    e.Cancel = true;
                }
            }
        }
    }
}
