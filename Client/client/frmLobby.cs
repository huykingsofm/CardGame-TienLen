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
        static int NUM_ROOM = 10;

        private TcpClientModel _client;
        private frmStartGame _parent;
        private String[] _room = new String[NUM_ROOM];
        private bool isStop;

        private void UpdateLobby(String[] rooms) {
            int index = 0;
            foreach(Control btn in flpnRooms.Controls) {
                btn.Tag = index;
                btn.Text = this._room[index++];
            }
        }

        private void HandleResponse(Object obj) {
            Message message = (Message)obj;
            String[] args = message.args;
            Console.WriteLine("From Lobby, handleResponse: " + message);

            switch (message.name) {
                case "LobbyInfo":
                    for(int i = 1; i <= 10; i ++) {
                        int num_Players = Convert.ToInt32(message.args[i * 3 - 2]);
                        int bet = Convert.ToInt32(message.args[i * 3 - 1]);
                        this._room[i - 1] = 
                            "Room: {0}\nPlayers: {1}\nBet: {2}Coins".Format(i, num_Players, bet);
                    }

                    //UI
                    this.UpdateLobby(this._room);
                    break;
                case "Success":
                    if(args[0] == "Logout") {
                        this.Hide();
                        this._parent.Show();
                    }
                    if(args[0] == "JoinRoom") {
                        this.Hide();
                        frmRoom frm = new frmRoom(this._client, this);
                        frm.ShowDialog();
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
            } catch(Exception ex) {
                MessageBox.Show(
                    "Can not join this room",
                    "Exception", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void frmLobby_Load(object sender, EventArgs e) {
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
            DialogResult dr = MessageBox.Show(
               "Exit Game ?",
               "Exit",
               MessageBoxButtons.YesNo,
               MessageBoxIcon.Question);

           if(dr == DialogResult.No) {
                e.Cancel = true;
           } else {
                String req = RequestFormat.EXIT_LOBBY();
                this._client.SendRequest(req);
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
            this.Close();
        }

        private void btnRule_Click(object sender, EventArgs e) {
            DialogResult dr = MessageBox.Show(
               "--Rules--",
               "Rules",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);
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
