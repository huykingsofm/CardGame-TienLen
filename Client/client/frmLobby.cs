using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Microsoft.VisualBasic;

namespace client
{
    public partial class frmLobby : Form
    {
        public frmLobby(TcpClientModel tcpModel)
        {
            InitializeComponent();
            this._client = tcpModel;
        }

        public frmLobby(TcpClientModel tcpModel, frmStartGame parent) {
            InitializeComponent();
            this._client = tcpModel;
            this._parent = parent;
        }

        public frmLobby(TcpClientModel tcpModel, frmStartGame parent, frmRoom child) {
            InitializeComponent();
            this._client = tcpModel;
            this._parent = parent;
            this._child = child;
        }

        static int NUM_ROOM = 10;

        private TcpClientModel _client;
        private frmStartGame _parent;
        private frmRoom _child;
        private String[] _room = new String[NUM_ROOM];
        private bool isStop;
        public int isWorking = 1;

        private void UpdateLobbyUI(String[] rooms) {
            int index = 0;
            foreach (Control btn in flpnRooms.Controls) {
                btn.Tag = index;
                btn.Text = this._room[index++];
            }
        }

        private void UpdateLobby(Object obj) {
            try {
                String[] args = (String[])obj;
                Console.WriteLine(args);
                for (int i = 1; i <= 10; i++) {
                    int num_Players = Convert.ToInt32(args[i * 3 - 2]);
                    int bet = Convert.ToInt32(args[i * 3 - 1]);
                    this._room[i - 1] =
                        "Room: {0}\nPlayers: {1}\nBet: {2}Coins".Format(i, num_Players, bet);
                }

                int index = 0;
                foreach (Control btn in flpnRooms.Controls) {
                    Console.WriteLine(btn);
                    flpnRooms.Invoke((MethodInvoker)delegate {
                        btn.Tag = index;
                        btn.Text = this._room[index++];
                        //btn.Click += this._onButtonCLick;
                    });
                }
            } catch (Exception ex) {
                //do something
            }
        }

        private void UpdateLobby(String[] args) {
            try {
                Console.WriteLine(args[0]);
                if (args.Length == 0) return;
                
                for (int i = 1; i <= 10; i++) {
                    int num_Players = Convert.ToInt32(args[i * 3 - 2]);
                    int bet = Convert.ToInt32(args[i * 3 - 1]);
                    this._room[i - 1] =
                        "Room: {0}\nPlayers: {1}\nBet: {2}Coins".Format(i, num_Players, bet);
                }

                this.UpdateLobbyUI(this._room);
            } catch (Exception ex) {
                //do something
            }
        }

        private void HandleResponse(Object obj) {
            Message message = (Message)obj;
            String[] args = message.args;
            Console.WriteLine("From Lobby, handleResponse: " + message);

            switch (message.name) {
                case "LobbyInfo":
                    this.UpdateLobby(args);
                    break;
                case "Success":
                    if(args[0] == "Logout") {
                        this.isStop = true;
                        this._parent.isWorking = 1;
                        this.Hide();

                        this._parent.Show();
                        this._parent.StartHandleReponses();
                    }
                    if(args[0] == "JoinRoom") {
                        this._parent.isWorking = 2;
                        this.isWorking = 2;
                        this.Hide();

                        frmRoom frm = new frmRoom(this._client, this);
                        frm.ShowDialog();  

                        if(this.isWorking == 1) {

                            //Thread th1 = new Thread(this.Show);
                            //th1.Start();
                            this.ShowDialog();
                            //this.StartHandleReponses();
                            //Thread th1 = new Thread(this.StartHandleReponses);
                            //th1.Start();

                            //String req = RequestFormat.GET_LOBBY_INFO();
                            //this._client.SendRequest(req);                                                                                                        
                        }                       
                    }
                    if(args[0] == "Payin") {
                        MessageBox.Show(
                            "Your current coins in the account are " + args[1] + " $c",
                            "Pay In",
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information
                            );
                    }
                    break;
                case "Failure":
                    MessageBox.Show(args[1], "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);                  
                    break;
                default:
                    Console.WriteLine("From lobby, an exception occured");
                    break;
            }
        }

        public void StartHandleReponses() {
            this.isStop = false;

            while (!isStop) {
                Thread.Sleep(300);
                Console.WriteLine("On Lobby Thread");
                Message m = MessageQueue.GetMessage();

                if (m == null) continue;

                this.HandleResponse(m);
            }
        }

        public void _onButtonCLick(object sender, EventArgs e) {
            try {
                Button btn = sender as Button;
                Console.WriteLine("From lobby, join room " + btn.Tag);
                int index = Convert.ToInt32(btn.Tag);
                String req = RequestFormat.JOIN_ROOM(index);

                this._client.SendRequest(req);

                Thread.Sleep(50);
            } catch(Exception ex) {
                MessageBox.Show(
                    "Can not join this room",
                    "Exception", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void frmLobby_Load(object sender, EventArgs e) {
            Console.WriteLine("frmLobby load-------------------------------");
            try {
                foreach(Control btn in flpnRooms.Controls) {
                    btn.Click += this._onButtonCLick;
                }

                Thread th = new Thread(this.StartHandleReponses);
                th.Start();
            } catch(Exception ex) {
                Console.WriteLine("frmLobby_Load error: " + ex.StackTrace);
            }
        }

        private void frmLobby_FormClosing(object sender, FormClosingEventArgs e) {
           if(this.isWorking == 0) {
                DialogResult dr = MessageBox.Show(
                  "Exit Game ?",
                  "Exit",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question);

                if (dr == DialogResult.No) {
                    e.Cancel = true;
                } else {
                    String req = RequestFormat.EXIT_LOBBY();
                    this._client.SendRequest(req);
                    //this._parent.isWorking = 1;
                }
           } else if(this.isWorking == 1) {
                String req = RequestFormat.EXIT_LOBBY();
                this._client.SendRequest(req);
                //this._parent.isWorking = 2;
            }
        }

        private void btnExit_Click(object sender, EventArgs e) {
            //String req = RequestFormat.EXIT_LOBBY();
            //this._client.SendRequest(req);

            //DialogResult dr = MessageBox.Show(
            //   "Exit Game ?",
            //   "Exit",
            //   MessageBoxButtons.YesNo,
            //   MessageBoxIcon.Question);

            //wtf
            //FormClosingEventArgs ev = e as FormClosingEventArgs;
            //this.frmLobby_FormClosing(sender, ev);
            this.isWorking = 0;
            this._parent.isWorking = 1;
            this.Close();
        }

        private void btnPayIn_Click(object sender, EventArgs e) {
            frmPayIn frm = new frmPayIn(this._client);
            frm.ShowDialog();
        }

        private void btnAbout_Click(object sender, EventArgs e) {
             MessageBox.Show(
               "Tiến Lên Miền Nam v1.0\nAuthors:\nLam Khac Duy\nLe Ngoc Huy\nAn Van Hieu",
               "About",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);
        }
    }
}
