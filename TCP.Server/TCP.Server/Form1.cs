using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// 1.匯入以下幾個命名空間
using System.Net; // 網路相關函數
using System.Net.Sockets; // 網路通訊物件函數
using System.Threading; // 多執行緒函數
using System.Collections;


// 在此要用到多執行緒，事實上在伺服器與多客戶同時連線時，
// 每一個客戶與伺服器之間都需要一個獨立的執行緒，連線數目越多就會有越多執行緒。

namespace TCP.Server
{
    public partial class Form1 : Form
    {
        // 2.宣告建立以下物件
        TcpListener Server; // 伺服器端網路監聽器
        Socket Client; // 給客戶用的連線物件
        Thread Th_Svr; // 伺服器監聽用執行緒(電話總機開放中)
        Thread Th_Clt; // 客戶用的通話執行緒(電話分機連線中)
        Hashtable HT; // 儲存客戶名稱與通訊物件的集合物件(雜湊表)(key:Name,Socket)

        // 說明
        // 1. Server(TcpListener)是專用於伺服端接受客戶連線要求的物件，可以想像它是一個電話總機。
        // 2. Server以一個獨立的執行緒 Th_Svr執行監聽工作，這樣其他表單物件才能正常工作，譬如顯示線上名單。
        // 3. Server 收到連線要求時會幫客戶建立一個Client連線物件，並建立一個獨立執行緒 Th_Clt讓客戶與伺服器保持溝通。
        // 4.但是每次建立的新客戶連線都是Client與 Th_Clt，不會重複嗎？事實上是程式會將兩者拷貝到新執行緒內部。因此兩者有如轉接用的計程車，可以重複使用！
        // 5. HT是一個稱為雜湊表(HashTable)的特殊集合物件，一般陣列是用序號取得成員，像A(2)表示陣列A的第三個元素；但是雜湊表是使用 "key" 來辨識成員，譬如HT的某個成員是：key="國文"，value=90。HT("國文")就等於90了！
        // 6. 本範例中 HT 用於存放所有線上客戶的連線物件(Socket)，key是使用者名稱，value 就是該使用者的連線物件。所以要與使用者"A"通訊的物件就是HT("A")了！
        public Form1()
        {
            InitializeComponent();
            this.HT = new Hashtable();

            this.button1.Click += ActivationServer;
            this.FormClosing += FormClose;
            this.textBox1.Text = "192.168.0.192";
            this.textBox2.Text = "222";
        }

        /// <summary>
        /// 啟動伺服器
        /// </summary>
        private void ActivationServer(object sender,EventArgs e) 
        {
            CheckForIllegalCrossThreadCalls = false; // 忽略跨執行緒處理的錯誤(允許跨執行緒存取變數)

            this.Th_Svr = new Thread(new ThreadStart(this.ServerSub)); // 宣告監聽執行緒(執行副程式 ServerSub)

            this.Th_Svr.IsBackground = true; // 設為背景執行緒

            this.Th_Svr.Start(); // 啟動執行緒

            this.button1.Enabled = false; // 讓按鍵無法使用(不能重複啟動伺服器)
        }

        /// <summary>
        /// 接收客戶連線要求的程式(如同電話總機)，針對每一個客戶建立一個連線，以及獨立執行緒
        /// </summary>
        private void ServerSub() 
        {
            var EP = new IPEndPoint(IPAddress.Parse(this.textBox1.Text), int.Parse(this.textBox2.Text)); // 設定 Server IP 與 Port

            this.Server = new TcpListener(EP); // 建立伺服器端監聽器(總機)

            Server.Start(10); // 啟動監聽設定允許最多連線數 10 人

            while (true) 
            {
                this.Client = this.Server.AcceptSocket(); // 建立此客戶的連線物件 Client
                this.Th_Clt = new Thread(new ThreadStart(Listen)); // 建立監聽這個客戶連線的獨立執行緒
                this.Th_Clt.IsBackground = true; // 設定為背景執行緒
                this.Th_Clt.Start(); // 開始執行緒運作
            }
        }

        /// <summary>
        /// 監聽客戶訊息的程式
        /// </summary>
        private void Listen()
        {
            Socket sck = this.Client; // 將公用的 Client 拷貝到 sck

            Thread th = this.Th_Clt; // 將公用的 Th_Clt 拷貝到執行緒 th

            while (true) 
            {
                try
                {
                    byte[] b = new byte[1023]; // 建立接收資料用的陣列，長度需大於可能的訊息
                    int inLen = sck.Receive(b); // 接收網路資訊(byte 陣列)
                    string msg = Encoding.Default.GetString(b, 0, inLen); // 翻譯實際訊息(長度 inLen)
                    string cmd = msg.Substring(0, 1); // 取出命令碼，第一個字
                    string str = msg.Substring(1); // 取出命令碼之後的訊息
                    if (cmd == "0") // 有新使用者上線:新增使用者到名單中
                    {
                        HT.Add(str, sck); // 連線資訊加入雜湊表，key: 使用者名稱，sck:連線物件 
                        this.listBox1.Items.Add(str); // 加入上線者名單
                    }

                    if (cmd == "9") // 使用者離線:移除客戶的名單與連線資訊，並結束執行緒與關閉連線
                    {
                        HT.Remove(str); // 移除使用者名稱為 str 的連線物件
                        this.listBox1.Items.Remove(str); // 自上線者名單移除 Name
                        th.Abort(); // 結束此客戶的監聽執行緒
                        sck.Close(); // 關閉此客戶的連線
                    }

                }
                catch (Exception ex) 
                {
                  // 有錯誤時忽略，通常是客戶無預警強制關閉程式，測試階段常發生
                }
            }
        }

        /// <summary>
        /// 表單關閉事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormClose(object sender, FormClosingEventArgs e) 
        {
            Application.ExitThread(); // 結束本專案的所有執行緒
        }
    }
}
