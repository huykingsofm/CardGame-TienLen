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
    public partial class frmPayIn : Form {
        public frmPayIn(TcpClientModel client) {
            InitializeComponent();
            this._client = client;
        }

        private TcpClientModel _client;

        private void btnPayIn_Click(object sender, EventArgs e) {
            try {
                String req = RequestFormat.PAY_IN(txtPayIn.Text);

                this._client.SendRequest(req);
                this.Close();
            } catch(Exception ex) {
                //do something
            }
        }
    }
}
