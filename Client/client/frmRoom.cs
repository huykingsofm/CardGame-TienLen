using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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

        private TcpClientModel _client;
        private Player _player;
        private frmLobby _parent;
        private bool isStop;

        private void UpdateRoom(String[] args) {
            try {
                //
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
                    //UpdateRoom
                    break;
                case "Success":
                    if (args[0] == "Logout") {
                        this.Hide();
                        this._parent.Show();
                    }
                    if (args[0] == "JoinRoom") {
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

        private void frmRoom_Load(object sender, EventArgs e) {
            try {

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
