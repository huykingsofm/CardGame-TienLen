using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
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

        static int MAX_TIME = 30;
        static int STEP = 1;
        static int CARD_WIDTH = 96;
        static int CARD_MIN_WIDTH = 64;
        static int CARD_HEIGHT = 150;
        static int CARD_MIN_HEIGHT = 100;
        static int Y_LOCATION = 600;
        static int X_BET_LOCATION = 790;
        static int Y_BET_LOCATION = 400;

        private int MovingTime = 0;
        private bool useAI = false;

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
                        c[2] = btnPlay;
                        c[3] = btnSkip;
                        c[4] = pbox0;
                        c[5] = pbar0;
                        c[6] = null;
                        break;
                    case 1:
                        c[0] = lblPlayerName1;
                        c[1] = lblRemainCoin1;
                        c[2] = lblRemainCards1;
                        c[3] = pboxRemainCards1;
                        c[4] = pbox1;
                        c[5] = pbar1;
                        c[6] = lblHost1;
                        break;
                    case 2:
                        c[0] = lblPlayerName2;
                        c[1] = lblRemainCoin2;
                        c[2] = lblRemainCards2;
                        c[3] = pboxRemainCards2;
                        c[4] = pbox2;
                        c[5] = pbar2;
                        c[6] = lblhost2;
                        break;
                    case 3:
                        c[0] = lblPlayerName3;
                        c[1] = lblRemainCoin3;
                        c[2] = lblRemainCards3;
                        c[3] = pboxRemainCards3;
                        c[4] = pbox3;
                        c[5] = pbar3;
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
                        if(c.Name.Substring(0, 4) == "pbar") {
                            c.Visible = false;
                        }
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
                        if (c.Name.Substring(0, 7) == "lblHost") continue;
                        c.Enabled = true;
                        if (c.Name.Substring(0, 4) == "pbar") {
                            c.Visible = true;
                        }
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
                for(int i = 4; i < 14; i += 3) {
                    String status = args[i];
                    if (status == "1") numPlayer++;

                    else if (status == "2") {
                        numPlayer++;
                        flag++;
                    }
                }

                if(flag > 0 || this.useAI) {
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

        private void UpdatePlayerByStatus(Control[] c, String[] playerInfo, bool isThisPlayer, bool isHost) {
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

                            //UI
                            //this.DisablePlayerUI(c);
                            break;
                        case 1:
                            c[0].Text = playerInfo[0] + " (UR)";
                            c[1].Text = playerInfo[1];
                            c[2].Text = "...";
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime

                            //UI
                            //this.EnablePlayerUI(c);
                            break;
                        case 2:
                            c[0].Text = playerInfo[0] + " (R)";
                            c[1].Text = playerInfo[1];
                            c[2].Text = "...";
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime

                            //UI
                            //this.DisablePlayerUI(c);
                            break;
                        case 3:
                            c[0].Text = playerInfo[0] + " (IG)";
                            c[1].Text = playerInfo[1];
                            (c[4] as PictureBox).Image = Image.FromFile(@"ingame-imgs/player2.png");
                            //c[5] for remainTime

                            //UI
                            //this.DisablePlayerUI(c);
                            break;
                    }

                    c[6].Invoke((MethodInvoker)delegate {
                        c[6].Visible = isHost;
                    });
                } else {
                    c[0].Text = playerInfo[0];
                    c[1].Text = playerInfo[1];

                    this._player.isHost = isHost;

                    //UI
                    //this.EnablePlayerUI(c);
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateRoom(Object obj) {
            try {
                String[] args = (String[])obj;

                int length = 12;
                String[] roomInfo = new String[length];
                Array.Copy(args, 2, roomInfo, 0, length);

                int hostIndex = Convert.ToInt32(args[1]);
                if (hostIndex == 0) {
                    this.ToggleStartButtonForHost(args);
                } else {
                    this.ToggleReadyUI();
                }

                int index = 0;
                for (int i = 2; i < 14; i += 3) {
                    String[] playerInfo = new String[3];
                    Array.Copy(args, i, playerInfo, 0, 3);
                    Control[] c = this.MapIndexToControls(index);

                    if (c != null) {
                        Console.WriteLine("--------------------------------------------isHost:{0}", hostIndex);
                        bool isThisPlayer = index == 0;
                        bool isHost = index == hostIndex;
                        this.UpdatePlayerByStatus(c, playerInfo, isThisPlayer, isHost);
                    }
                    index++;
                }
            } catch (Exception ex) {
                //do something
                Console.WriteLine("From room, an exception occurred");
            }
        }

        private void UpdateRoom(String[] args) {
            try {
                int length = 12;
                String[] roomInfo = new String[length];
                Array.Copy(args, 2, roomInfo, 0, length);

                int hostIndex = Convert.ToInt32(args[1]);
                if(hostIndex == 0) {
                    this._player.isHost = true;
                    this.ToggleStartButtonForHost(args);
                } else {
                    this._player.isHost = false;
                    this.ToggleReadyUI();
                }

                int index = 0;
                for (int i = 2; i < 14; i += 3) {
                    String[] playerInfo = new String[3];
                    Array.Copy(args, i, playerInfo, 0, 3);
                    Control[] c = this.MapIndexToControls(index);

                    if (c != null) {
                        bool isThisPlayer = index == 0;
                        bool isHost = index == hostIndex;
                        this.UpdatePlayerByStatus(c, playerInfo, isThisPlayer, isHost);
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
        
        private void RemovePlayedCards() {
            try {
                foreach(PictureBox pb in this._playedCards) {
                    this.Invoke((MethodInvoker)delegate {
                        this.Controls.Remove(pb);
                    });
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void FacePlayedCardsDown() {
            try {
                SoundPlayer SPlayer = new SoundPlayer(@"audio\play1.wav");

                foreach (PictureBox pb in this._playedCards) {
                    SPlayer.Play();

                    this.Invoke((MethodInvoker)delegate {
                        pb.Image = Image.FromFile(@"ingame-imgs/card-down.png");
                        pb.BackColor = Color.Transparent;
                    });
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

        private void PlayLeftUI(String cards, Control[] c) {
            try {
                if(c != null) {
                    Console.WriteLine("---------PlayLeftUI " + cards + "----------------------------------------");

                    //while (pb1.Location.X < X_BET_LOCATION) {
                    //    Console.WriteLine("-------------pb1.X: {0} - {1}", pb1.Location.X, X_BET_LOCATION);

                    //    pb1.Invoke((MethodInvoker)delegate {
                    //        pb1.Location = new Point(pb1.Location.X + 5, pb1.Location.Y);
                    //        pb1.Visible = true;
                    //    });
                    //}

                    //pb1.Invoke((MethodInvoker)delegate {
                    //    pb1.Location = pboxRemainCards1.Location;
                    //    pb1.Visible = false;
                    //});

                    //Thread th = new Thread(this.PlayUI);
                    //th.Start(cards);
                    this.PlayUI(cards);
                }
            } catch(Exception ex) {
                //do something
                Console.WriteLine("PlayLeftUI error: {0}", ex);
            }
        }

        private void PlayRightUI(String cards, Control[] c) {
            try {
                if (c != null) {
                    Console.WriteLine("---------PlayRightUI " + cards + "----------------------------------------");
                    //while (pb3.Location.X > X_BET_LOCATION) {
                    //    Console.WriteLine("-------------pb3.X: {0} - {1}", pb3.Location.X, X_BET_LOCATION);
                    //    pb3.Invoke((MethodInvoker)delegate {
                    //        pb3.Location = new Point(pb3.Location.X - 5, pb3.Location.Y);
                    //    });
                    //}

                    //pb3.Invoke((MethodInvoker)delegate {
                    //    pb3.Location = pboxRemainCards3.Location;
                    //    pb3.Visible = false;
                    //});

                    //Thread th = new Thread(this.PlayUI);
                    //th.Start(cards);
                    this.PlayUI(cards);
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void PlayUpUI(String cards, Control[] c) {
            try {
                if (c != null) {
                    //while (pb2.Location.Y < Y_BET_LOCATION) {
                    //    Console.WriteLine("-------------pb2.X: {0} - {1}", pb2.Location.X, X_BET_LOCATION);
                    //    pb2.Invoke((MethodInvoker)delegate {
                    //        pb2.Location = new Point(pb2.Location.X, pb2.Location.Y + 5);
                    //    });
                    //}

                    //pb2.Invoke((MethodInvoker)delegate {
                    //    pb2.Location = pboxRemainCards2.Location;
                    //    pb2.Visible = false;
                    //});                    

                    //Thread th = new Thread(this.PlayUI);
                    //th.Start(cards);
                    this.PlayUI(cards);
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void PlayDownUI(String cards, Control[] c) {
            try {
                if (c != null) {
                    //Point initPos = pb0.Location;
                    //while (pb0.Location.Y > Y_BET_LOCATION) {
                    //    Console.WriteLine("-------------pb0.X: {0} - {1}", pb0.Location.X, X_BET_LOCATION);

                    //    pb0.Invoke((MethodInvoker)delegate {
                    //        pb0.Location = new Point(pb0.Location.X, pb0.Location.Y - 5);
                    //    });
                    //}

                    //pb0.Invoke((MethodInvoker)delegate {
                    //    pb0.Location = initPos;
                    //    pb0.Visible = false;
                    //});

                    //Thread th = new Thread(this.PlayUI);
                    //th.Start(cards);
                    this.PlayUI(cards);
                    //String[] cs = cards.Split(',');
                    //this.UpdateMyCardsUI(cs);
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void PlayUI(Object obj) {
            try {
                String cards = (String)obj;

                Console.WriteLine("-------------------------playUI" + cards);
                SoundPlayer SPlayer = new SoundPlayer(@"audio\play1.wav");
                Random rand = new Random();

                String[] playCards = cards.Split(',');
                int numCards = playCards.Length;
                int begPosY = Y_BET_LOCATION + rand.Next(1, 20) - rand.Next(1, 20);
                int begPosX = X_BET_LOCATION + (numCards * (CARD_MIN_WIDTH - 5)) / 2 + rand.Next(0, 15) - rand.Next(1, 15);

                PictureBox[] c = new PictureBox[numCards];
                for (int i = numCards - 1; i >= 0; i--) {
                    int posX = i == numCards - 1 ? begPosX : c[i + 1].Location.X - CARD_WIDTH + 8;
                    c[i] = new PictureBox();
                    c[i].Width = CARD_MIN_WIDTH;
                    c[i].Height = CARD_MIN_HEIGHT;
                    c[i].Name = playCards[i];

                    Image img = Image.FromFile(@"cards/" + c[i].Name + ".png");
                    c[i].Image = img;
                    c[i].SizeMode = PictureBoxSizeMode.StretchImage;

                    c[i].Location = new Point(posX, begPosY);
                }

                this._playedCards.AddRange(c);

                SPlayer.Play();

                this.Invoke((MethodInvoker)delegate {
                    this.Controls.AddRange(c);
                });
            } catch (Exception ex) {
                //do something
            }
        }

        private void PlayUI(String cards) {
            try {
                if (cards.Length == 0) return;

                Console.WriteLine("-------------------------playUI" + cards);
                SoundPlayer SPlayer = new SoundPlayer(@"audio\play1.wav");
                Random rand = new Random();

                String[] playCards = cards.Split(',');
                int numCards = playCards.Length;
                int begPosY = Y_BET_LOCATION + rand.Next(3, 15) - rand.Next(0, 20);
                Console.WriteLine("-------------------RANDOM: {0}", begPosY);
                int begPosX = X_BET_LOCATION + (numCards * (CARD_MIN_WIDTH - 5)) / 2 - CARD_MIN_WIDTH;

                this.RemovePlayedCards();

                PictureBox[] c = new PictureBox[numCards];
                for (int i = numCards - 1; i >= 0; i--) {
                    int posX = i == numCards - 1 ? begPosX : c[i + 1].Location.X - CARD_MIN_WIDTH + 8;
                    c[i] = new PictureBox();
                    c[i].Width = CARD_MIN_WIDTH;
                    c[i].Height = CARD_MIN_HEIGHT;
                    c[i].Name = playCards[i];
                    c[i].Visible = true;
                    c[i].BackColor = Color.Transparent;
                    Image img = Image.FromFile(@"cards/" + c[i].Name + ".png");
                    c[i].Image = img;
                    c[i].SizeMode = PictureBoxSizeMode.StretchImage;

                    c[i].Location = new Point(posX, begPosY);

                    this.Invoke((MethodInvoker)delegate {
                        this.Controls.Add(c[i]);
                    });
                }

                this._playedCards.AddRange(c);

                SPlayer.Play();
            } catch(Exception ex) {
                //do something
                Console.WriteLine("PlayUI error: {0}", ex);
            }
        }

        private void PlayCards(String[] args) {
            try {
                int numCards = args.Length - 1;
                int playerIndex = Convert.ToInt32(args[0]);
                String[] tmp = new String[numCards];
                Array.Copy(args, 1, tmp, 0, numCards);

                Control[] c = this.MapIndexToControls(playerIndex);
                ProgressBar pb = c[5] as ProgressBar;


                pb.Invoke((MethodInvoker)delegate {
                    pb.Visible = false;
                });

                String cards = String.Join(",", tmp);

                Console.WriteLine("----------------------------PLAYCARDS------------------- " + playerIndex + " - " + numCards);
                switch(playerIndex) {
                    case 0:
                        this.PlayDownUI(cards, c);
                        break;
                    case 1:
                        this.PlayLeftUI(cards, c);
                        break;
                    case 2:
                        this.PlayUpUI(cards, c);
                        break;
                    case 3:
                        this.PlayRightUI(cards, c);
                        break;
                    default:
                        break;
                }
            } catch(Exception ex) {
                //do something
                Console.WriteLine("PlayCards error: {0}", ex);
            }
        }

        private void RemoveCurrentCards() {
            try {
                foreach(Control c in this._cards) {
                    this.Invoke((MethodInvoker)delegate {
                        this.Controls.Remove(c);
                    });
                    //this.Controls.Remove(c);
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateMyCardsUI(String[] cards) {
            try {
                Console.WriteLine("---------------------UPDATE_MY_CARDS_UI {0}", cards.Length);
                int numCards = cards.Length;
                int begPosY = Y_LOCATION;
                int begPosX = X_BET_LOCATION + (numCards * (CARD_WIDTH - 20)) / 2 - CARD_WIDTH;

                this.RemoveCurrentCards();

                PictureBox[] c = new PictureBox[numCards];
                for (int i = numCards - 1; i >= 0; i--) {
                    Console.WriteLine(i);
                    int posX = i == numCards - 1 ? begPosX : c[i + 1].Location.X - CARD_WIDTH + 20;
                    c[i] = new PictureBox();
                    c[i].Width = CARD_WIDTH;
                    c[i].Height = CARD_HEIGHT;
                    c[i].Name = cards[i];
                    c[i].Tag = 0;
                    c[i].Visible = true;
                    Image img = Image.FromFile(@"cards/" + c[i].Name + ".png");
                    c[i].Image = img;
                    c[i].SizeMode = PictureBoxSizeMode.StretchImage;
                    c[i].BackColor = Color.Transparent;
                    c[i].Location = new Point(posX, begPosY);
                    c[i].Click += this._card_Click;

                    this.Invoke((MethodInvoker)delegate {
                        this.Controls.Add(c[i]);
                    });

                    Console.WriteLine(c[i]);
                }

                this._cards = c;

                //this.Invoke((MethodInvoker)delegate {
                //    this.Controls.AddRange(c);
                //});
            } catch(Exception ex) {
                //do something
                Console.WriteLine("UPDATE_MY_CARDS_UI: " + ex);
            }
        }

        private void UpdateOpponentCards(String[] args, int n) {
            try {
                Console.WriteLine("---------------update component cards");
                for(int i = 1; i <= 3; i++) {
                    Control[] c = this.MapIndexToControls(i);
                    c[2].Text = args[n + i];
                    c[5].Visible = false;
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateCardsUI(String[] args, int playerIndex) {
            try {
                Console.WriteLine("-------------------UPDATE_CARD_UI - {0}", args.Length);
                int numCards = Convert.ToInt32(args[0]);
                String[] cards = new String[numCards];

                Array.Copy(args, 1, cards, 0, numCards);

                //if(playerIndex == 0) {
                //    this._player.UpdateCards(cards);
                //    this.UpdateMyCardsUI(cards);
                //}
                this._player.UpdateCards(cards);
                this.UpdateMyCardsUI(cards);
                this.UpdateOpponentCards(args, numCards);
            } catch(Exception ex) {
                //do something
            }
        }

        private void UpdateRemainTime(String[] args) {
            try {
                Console.WriteLine(args[0]);
                int playerIndex = Convert.ToInt32(args[0]);
                int rTime = Convert.ToInt32(args[1]);
                Console.WriteLine("-------------------------" + rTime);
                Control[] c = this.MapIndexToControls(playerIndex);
                if (c != null) {
                    Console.WriteLine("----------------------- in update time----------------");
                    ProgressBar pbar = (c[5] as ProgressBar);
                    pbar.Invoke((MethodInvoker)delegate {
                        pbar.Visible = true;
                        if (rTime <= 1) pbar.Visible = false;
                        pbar.Maximum = MAX_TIME;
                        pbar.Step = STEP;
                        pbar.Value = rTime;
                    });
                }
            } catch(Exception ex) {
                //do something
                Console.WriteLine("UpdateRemainTime error: {0}", ex);
            }
        }

        private void UpdateRemainTime(Object obj) {
            try {
                String[] args = (String[])obj;
                Console.WriteLine(args[0]);
                int playerIndex = Convert.ToInt32(args[0]);
                int rTime = Convert.ToInt32(args[1]);
                Console.WriteLine("-------------------------" + rTime);
                Control[] c = this.MapIndexToControls(playerIndex);
                if(c != null) {
                    ProgressBar pbar = (c[5] as ProgressBar);
                    pbar.Invoke((MethodInvoker)delegate {
                        pbar.Visible = true;
                        if (rTime <= 1) pbar.Visible = false;
                        pbar.Maximum = MAX_TIME;
                        pbar.Step = STEP;
                        pbar.Value = rTime;
                    });
                }
            } catch (Exception ex) {
                //do something
                Console.WriteLine("UpdateRemainTime error: {0}", ex);
            }
        }

        private void UpdateGame(Object obj) {
            try {
                String[] args = (String[])obj;

                int numCards = Convert.ToInt32(args[0]);
                int playerIndex = Convert.ToInt32(args[numCards + 4]);
                bool couldPass = Convert.ToInt32(args[numCards + 5]) == 0;

                Console.WriteLine("--------------PlayerIndex: {0}", playerIndex);
                if (playerIndex == 0) {
                    btnPlay.Invoke((MethodInvoker)delegate {
                        btnPlay.Visible = true;
                    });
                    btnSkip.Invoke((MethodInvoker)delegate {
                        btnSkip.Visible = true;
                    });
                } else if (couldPass) {
                    btnSkip.Invoke((MethodInvoker)delegate {
                        btnSkip.Visible = true;
                    });
                } else {
                    //btnPlay.Visible =
                    //btnSkip.Visible = false;
                    //this._player.Played();
                }
                this.UpdateCardsUI(args, playerIndex);
            } catch (Exception ex) {
                //do something
            }
        }

        private void UpdateGame(String[] args) {
            try {
                int numCards = Convert.ToInt32(args[0]);
                int playerIndex = Convert.ToInt32(args[numCards + 4]);
                bool couldPass = Convert.ToInt32(args[numCards + 5]) == 0;

                Console.WriteLine("----------------------updategame------------");

                Console.WriteLine("--------------PlayerIndex: {0}", playerIndex);
                Console.WriteLine("--------------CouldPass: {0}", couldPass);

                if (playerIndex == 0 || couldPass) {
                    btnPlay.Visible = true;
                    btnSkip.Visible = true;
                } else if (couldPass) {
                    btnSkip.Visible = true;
                }
                //} else {
                //    btnSkip.Visible = true;
                //}
                this.UpdateCardsUI(args, playerIndex);
            } catch(Exception ex) {
                //do something
            }
        }

        private void FinishGame(String[] args) {
            try {
                if(args[0] == "0") {
                    frmWinner frm = new frmWinner();
                    frm.ShowDialog();
                } else {
                    frmLost frm = new frmLost();
                    frm.ShowDialog();
                }

                this.RemovePlayedCards();
                
                this.Invoke((MethodInvoker)delegate {
                    btnStartGame.Enabled = true;
                    btnReady.Enabled = true;
                    pbar0.Visible = false;
                });
            } catch(Exception ex) {
                //do somrthing
            }
        }
        
        private void HandleResponse(Object obj) {
            try {
                Message message = (Message)obj;
                String[] args = message.args;
                Console.WriteLine("From Room, handleResponse: " + message);
                Console.WriteLine(message.name);
                switch (message.name) {
                    case "RoomInfo":
                        Thread th = new Thread(this.UpdateRoom);
                        th.Start(args);
                        //this.UpdateRoom(args);
                        break;
                    case "Success":
                        if (args[0] == "Play" || args[0] == "Pass") {
                            this._player.Played();
                        }
                        break;
                    case "Failure":
                        if (args[0] == "Ready") {
                            this.ReadyFailed(args[1]);
                        }
                        if (args[0] == "UnReady") {
                            this.UnReadyFailed(args[1]);
                        }
                        if (args[0] == "Start") {
                            this.StartFailed(args[1]);
                        }
                        break;
                    case "Time":
                        //Thread thT = new Thread(this.UpdateRemainTime);
                        //thT.Start();
                        this.UpdateRemainTime(args);
                        break;
                    case "GameInfo":
                        //Thread thG = new Thread(this.UpdateGame);
                        //thG.Start(args);
                        this.UpdateGame(args);
                        break;
                    case "PlayingCard":
                        this.PlayCards(args);
                        break;
                    case "GameFinished":
                        this.FinishGame(args);
                        break;
                    default:
                        break;
                }
            } catch(Exception ex) {
                Console.WriteLine("Handle Response error: {0}", ex);
            }
            //String[] tmpArgs = { "13", "3_1", "4_2", "5_3", "6_4", "7_2", "8_1", "9_1", "10_4", "11_2", "12_1", "13_2", "1_1", "2_2", "12", "10", "0", "1", "-1" };
            //this.UpdateGame(tmpArgs);

        }

        public void StartHandleReponses() {
            this.isStop = false;

            while (!isStop) {
                Thread.Sleep(50);

                Message m = MessageQueue.GetMessage();
                if (m == null) continue;

                Console.WriteLine("StartHandleResponsses: {0} - {1}", m, m.args[0]);
                ////this.HandleResponse(m);
                Thread th = new Thread(this.HandleResponse);
                th.Start(m);
            }
        }

        private void frmRoom_Load(object sender, EventArgs e) {
            CheckForIllegalCrossThreadCalls = false;
            
            Thread th = new Thread(this.StartHandleReponses);
            th.Start();
        }

        private void frmRoom_FormClosing(object sender, FormClosingEventArgs e) {
            DialogResult dr = MessageBox.Show(
                     "If you exit game, you will lose your coin in this game. Still exit ?",
                     "Exit Room",
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question
                     );

            if (dr == DialogResult.No) {
                e.Cancel = true;
            } else {
                this.isStop = true;
                this._parent.isWorking = 1;               
                this._player.ExitGame();
                //this._parent.Visible = true;
                //this.Visible = false;
            }
        }
        
        private void btnReady_Click(object sender, EventArgs e) {
            try {
                bool isReady = this._player.isReady;

                if(isReady) {
                    this._player.UnReady();

                    //UI
                    btnReady.Text = "READY";
                } else {
                    this._player.Ready();

                    //UI
                    btnReady.Text = "UN_READY";
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void btnStartGame_Click(object sender, EventArgs e) {
            try {
                if(this._player.isHost) {
                    //if(this._playedCards.Count() > 0) {
                    //    this.RemovePlayedCards();
                    //}
                    this._player.StartGame();

                    this.Invoke((MethodInvoker)delegate {
                        btnStartGame.Enabled = false;
                        btnReady.Enabled = false;
                    });
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void btnExitRoom_Click(object sender, EventArgs e) {
            try { 
                this.Close();
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                //do something
            }
        }

        private void btnPlay_Click(object sender, EventArgs e) {
            try {
                String cards = this.GetSelectedCards();
                if(cards != null) {
                    this._player.Play(cards);
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void btnSkip_Click(object sender, EventArgs e) {
            try {
                this._player.Skip();
                pbar0.Invoke((MethodInvoker)delegate {
                    pbar0.Visible = false;
                });
            } catch (Exception ex) {
                //do something
            }
        }

        private void _card_Click(object sender, EventArgs e) {
            try {
                PictureBox pb = sender as PictureBox;

                int posX = pb.Location.X;
                int posY = pb.Location.Y;

                if(Convert.ToInt32(pb.Tag) == 0) {
                    pb.Location = new Point(posX, posY - 20);
                    pb.Tag = 1;
                } else {
                    pb.Location = new Point(posX, posY + 20);
                    pb.Tag = 0;
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            int posX = pb1.Location.X + 10;
            int posY = pb1.Location.Y;
            pb1.Visible = true;
            pb1.Location = new Point(posX, posY);
            Console.WriteLine("timer1------------------------------------------ {0} - {1}", posX, posY);
            if(posX >= X_BET_LOCATION) {
                
                timer1.Stop();
            }
        }

        private void timer2_Tick(object sender, EventArgs e) {
            int posX = pb2.Location.X;
            int posY = pb2.Location.Y + 1;
            pb2.Location = new Point(posX, posY);

            if (posY >= Y_BET_LOCATION) {
                timer2.Stop();
            }
        }

        private void timer3_Tick(object sender, EventArgs e) {
            int posX = pb3.Location.X - 10;
            int posY = pb3.Location.Y;
            pb3.Location = new Point(posX, posY);

            if (posX <= X_BET_LOCATION) {
                timer3.Stop();
            }
        }

        private void timer4_Tick(object sender, EventArgs e) {
            int posX = pb2.Location.X;
            int posY = pb2.Location.Y - 1;
            pb2.Location = new Point(posX, posY);
            Console.WriteLine("100");

            if (posY <= Y_BET_LOCATION) {
                timer4.Stop();
            }
        }

        private void btnTest_Click(object sender, EventArgs e) {
            MessageBox.Show("ok!", "lol", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void pbox1_Click(object sender, EventArgs e) {
            try {
                if (!this._player.isHost) return;

                if (pbox1.Tag.ToString() == "human") {
                    this._player.SetAI(1);
                    this.useAI = true;
                    pbox1.Tag = "ai";

                    //UI
                    pbox1.Image = Image.FromFile(@"ingame-imgs/ai.png");
                } else {
                    this._player.RemoveAI(1);
                    this.useAI = false;
                    pbox1.Tag = "human";

                    //UI
                    pbox1.Image = Image.FromFile(@"ingame-imgs/player2.png");
                }
            } catch(Exception ex) {
                //do something
            }
        }

        private void pbox2_Click(object sender, EventArgs e) {
            try {
                if (!this._player.isHost) return;

                if (pbox2.Tag.ToString() == "human") {
                    this._player.SetAI(2);
                    this.useAI = true;
                    pbox2.Tag = "ai";

                    //UI
                    pbox2.Image = Image.FromFile(@"ingame-imgs/ai.png");
                } else {
                    this._player.RemoveAI(2);
                    this.useAI = false;
                    pbox2.Tag = "human";

                    //UI
                    pbox2.Image = Image.FromFile(@"ingame-imgs/player2.png");
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void pbox3_Click(object sender, EventArgs e) {
            try {
                if (!this._player.isHost) return;

                if (pbox3.Tag.ToString() == "human") {
                    this._player.SetAI(3);
                    this.useAI = true;
                    pbox3.Tag = "ai";

                    //UI
                    pbox3.Image = Image.FromFile(@"ingame-imgs/ai.png");
                } else {
                    this._player.RemoveAI(3);
                    this.useAI = false;
                    pbox3.Tag = "human";

                    //UI
                    pbox3.Image = Image.FromFile(@"ingame-imgs/player2.png");
                }
            } catch (Exception ex) {
                //do something
            }
        }
    }
}
