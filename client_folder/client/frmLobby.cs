using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class frmLobby : Form
    {
        public frmLobby(TcpClientModel tcpModel)
        {
            InitializeComponent();
            this._client = tcpModel;
        }

        private TcpClientModel _client;
        private int _numRoom = 0;
        private int _roomIndex = 0;

        private void frmLobby_Load(object sender, EventArgs e) {
            try {
                //get lobbies from server
                String req = RequestFormat.GET_ROOMS();
                this._client.SendRequest(req);
                String res = this._client.ReceiveResponse();
                
                //render lobbies
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

            //wtf
            if (dr == DialogResult.No) {
                this.Close();
            }
        }
    }
}
