using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// 1.匯入命名空間
using System.Net; // 網路相關函數
using System.Net.Sockets; // 網路通訊物件函數

// 匯入的命名空間少了執行緒相關的System.Threading，
// 因為本程式只負責發出上線與離線訊息，
// 並不做接聽的動作，因此暫時不需要額外的執行緒。

namespace TCP.Client
{
    public partial class Form1 : Form
    {
        Socket T; // 網路連線物件
        string user; // 使用者名稱

        public Form1()
        {
            InitializeComponent();
            this.button1.Click += LoginServer;
            this.FormClosing += FormClose;
            this.textBox1.Text = "192.168.0.192";
            this.textBox2.Text = "222";
        }

        /// <summary>
        /// 登入伺服器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginServer(object sender,EventArgs e) 
        {
            string ip = this.textBox1.Text; // 伺服器 IP
            string port = this.textBox2.Text; // 伺服器 Port
            var EP = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)); // 伺服器的連線端點資訊，建立通訊物件，參數代表可以雙向通訊的 TCP 連線
            this.T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.user = this.textBox3.Text;

            try
            {
                this.T.Connect(EP); // 連上伺服器的端點 EP (類似撥號給電話總機)
                this.Send("0" + this.user); // 連線後隨即傳送自己的名稱給伺服器
            }
            catch (Exception ex) 
            {
                MessageBox.Show("無法連上伺服器!");
                return;
            }

            this.button1.Enabled = false; // 連線成功後讓連線按鍵失效，避免重複連線
        }

        /// <summary>
        /// 傳送訊息給 Server
        /// </summary>
        /// <param name="str"></param>
        private void Send(string str) 
        {
            byte[] b = Encoding.Default.GetBytes(str); // 翻譯文字為 Byte 陣列
            this.T.Send(b, 0, b.Length, SocketFlags.None); // 使用連線物件傳送資料
        }

        /// <summary>
        /// 表單關閉事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormClose(object sender,FormClosingEventArgs e) 
        {
            if (this.button1.Enabled) return;

            this.Send("9" + this.user); // 傳送自己的離線訊息給伺服器
            this.T.Close(); // 關閉網路通訊
        }
    }
}
