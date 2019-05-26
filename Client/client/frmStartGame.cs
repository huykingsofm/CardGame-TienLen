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
using System.Media;

namespace client {
    public partial class frmStartGame : Form {
        public frmStartGame() {
            InitializeComponent();
        }

        static int MAX_PERCENT = 100;

        private bool isStop;
        private TcpClientModel _client = new TcpClientModel();
        private int _progressPercent = 0; //To display progressbar
        private SoundPlayer _sPlayer = new SoundPlayer(@"audio\background-audio.wav"); //To play audio when playing game 

        private void UpdateProgress() {
            Random rd = new Random();
            int step = rd.Next(1, 5);
            int newPercent = this._progressPercent + step;

            if(newPercent >= 100) {
                this._progressPercent = 100;

                //UI
                pBar.Visible = false;
                lblWelcome.Visible = true;

                return;
            }

            this._progressPercent = newPercent;

            //UI
            pBar.Value = this._progressPercent;
        }

        private int _onFormLoad() {
            //UI
            lblUserSignUp.Visible =
            lblUserLogin.Visible =
            lblUsername.Visible =
            lblPassword.Visible =
            lblPassword2.Visible =
            lblWelcome.Visible = false;

            txtUsername.Visible =
            txtPassword.Visible =
            txtPassword2.Visible = false;

            btnLogin.Visible =
            btnSignUp.Visible =
            btnLoginSubmit.Visible =
            btnSignUpSubmit.Visible = false;

            this.Text = "Loading...";

            int isSuccess = this._client.ConnectToServer();
            if (isSuccess == 1) {
                MessageQueue.Start(this._client);
            }

            return isSuccess;
        }

        private void _onSignUpClick() {
            //UI
            btnLogin.Visible = true;
            btnSignUp.Visible = true;
            btnLoginSubmit.Visible = false;
            btnSignUpSubmit.Visible = true;

            lblUserSignUp.Visible =
            lblUsername.Visible =
            lblPassword.Visible =
            lblPassword2.Visible =
            txtUsername.Visible =
            txtPassword.Visible =
            txtPassword2.Visible = true;

            lblUserLogin.Visible = false;
        }

        private void _onLoginClick() {
            //UI
            btnLogin.Visible = true;
            btnSignUp.Visible = true;
            btnLoginSubmit.Visible = true;
            btnSignUpSubmit.Visible = false;

            lblUserLogin.Visible =
            lblUsername.Visible =
            lblPassword.Visible =
            txtUsername.Visible =
            txtPassword.Visible = true;

            lblPassword2.Visible = 
            txtPassword2.Visible = 
            lblUserLogin.Visible = false;
        }

        private void HandleResponse(Object obj) {
            Message message = (Message)obj;
            Console.WriteLine("From start game, handleResponse: " + message);
            
            switch(message.name) {
                case "Success":
                    if(message.args == null || message.args.Count() != 1) {
                        break;
                    }
                    if(message.args[0] == "Login") {
                        this.isStop = true;
                        this.Hide();

                        frmLobby frm = new frmLobby(this._client);
                        frm.ShowDialog();
                    }
                    if(message.args[0] == "Signup") {
                        MessageBox.Show("" +
                            "Sign up successfully!", 
                            "Notification", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                    break;
                case "Failure":
                    MessageBox.Show("" +
                            message.args[0],
                            "Notification",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    break;
                default:
                    Console.WriteLine("HandleResponse exception occurred");
                    break;
            }
        }

        public void StartHandleReponses() {
            this.isStop = false;

            while(!isStop) {
                Message m = MessageQueue.GetMessage();
                if (m == null) continue;

                //Thread th = new Thread(this.HandleResponse);
                //th.Start(m);
                this.HandleResponse(m);
            }
        }

        public void StopHandleResponses() {
            //do something
        }

        private void frmStartGame_Load(object sender, EventArgs e) {
            CheckForIllegalCrossThreadCalls = false;

            lblWelcome.Visible = false;
            pBar.Maximum = MAX_PERCENT;

            int success = this._onFormLoad();
            if(success != 1) {
                MessageBox.Show(
                    "Connection failed!", 
                    "Exception", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
            } else {
                this.timer1.Start();
                Thread th = new Thread(this.StartHandleReponses);
                th.Start();
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            this.UpdateProgress();

            if(this._progressPercent == 100) {
                this.timer1.Stop();

                //this._onMarquee();
                this.Text = "Tiến Lên Miền Nam";
                this._onSignUpClick();
                this._sPlayer.PlayLooping();
            }
        }

        private void frmStartGame_FormClosing(object sender, FormClosingEventArgs e) {
            DialogResult dr = MessageBox.Show(
                "Exit Game ?", 
                "Exit", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            //wtf
            if(dr == DialogResult.No) {
                this.Close();
            }
        }

        private void btnSignUp_Click(object sender, EventArgs e) {
            this._onSignUpClick();
        }

        private void btnLogin_Click(object sender, EventArgs e) {
            this._onLoginClick();
        }

        private void btnLoginSubmit_Click(object sender, EventArgs e) {
            try {
                String username = txtUsername.Text;
                String password = txtPassword.Text;

                String req = RequestFormat.LOG_IN(username, password);

                this._client.SendRequest(req);
                //String res = this._client.ReceiveResponse();
                //String []arr = res.Split(new char[] { ':', '|'});

                //if(arr[0] == "Success") {
                //    frmLobby frm = new frmLobby(this._client);
                //    frm.ShowDialog();
                //} else {
                //    throw new Exception(arr[1]);
                //}
            } catch(Exception ex) {
                Console.WriteLine("btnLoginSubmit_Click error: " + ex.StackTrace);
                //MessageBox.Show(
                //    ex.Message,
                //    "Exception",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Warning);
            }
        }

        private void btnSignUpSubmit_Click(object sender, EventArgs e) {
            try {
                String username = txtUsername.Text;
                String password = txtPassword.Text;
                String password2 = txtPassword2.Text;

                if(password != password2) {
                    throw new Exception("Passwords not match");
                }

                String req = RequestFormat.SIGN_UP(username, password);

                this._client.SendRequest(req);
                //String res = this._client.ReceiveResponse();
                //if (res != null) {
                //    frmLobby frm = new frmLobby(this._client);
                //    frm.ShowDialog();
                //}
            } catch (Exception ex) {
                Console.WriteLine("btnSignUpSubmit_Click error: " + ex.StackTrace);
                MessageBox.Show(
                    ex.Message,
                    "Exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
